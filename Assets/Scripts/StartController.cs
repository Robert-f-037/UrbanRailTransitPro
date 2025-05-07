using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//using System.Windows.Forms;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PimDeWitte.UnityMainThreadDispatcher;

public class StartController : MonoBehaviour
{
    public List<GameObject> sceneButtons;
    public Image window;
    private string sceneName = "";
    private UnityEngine.UI.Button ODButton;
    private UnityEngine.UI.Button TrainButton;
    private Button StartprocessButton;
    private Slider processSlider;
    private Button simlationButton;
    private GameObject loadSceneMask;
    //private string[] ODfilesPath = new string[0];
    //private string[] TrainfilesPath = new string[0];
    //private OpenFileDialog openFileDialog = new OpenFileDialog();

    void Start()
    {
        GameObject gameObject = GameObject.Find("GameManager");
        if (gameObject)
        {
            Destroy(gameObject);
        }

        foreach (GameObject sceneButton in sceneButtons)
        {
            sceneButton.GetComponent<Button>().onClick.AddListener(() => WindowStart(sceneButton));
        }
        ODButton = window.transform.Find("OD").GetComponent<UnityEngine.UI.Button>();
        TrainButton = window.transform.Find("Train").GetComponent<UnityEngine.UI.Button>();
        ODButton.onClick.AddListener(() => OpenFileExplorer(ODButton));
        TrainButton.onClick.AddListener(() => OpenFileExplorer(TrainButton));
        window.transform.Find("cancel").GetComponent<Button>().onClick.AddListener(WindowEnd);
        StartprocessButton = window.transform.Find("Startprocess").GetComponent<Button>();
        StartprocessButton.onClick.AddListener(async() => await ProcessStart());
        simlationButton = window.transform.Find("Startsimulation").GetComponent<Button>();
        simlationButton.onClick.AddListener(SimulationStart);
        processSlider = StartprocessButton.transform.Find("Slider").GetComponent<Slider>();
        loadSceneMask = window.transform.Find("loadscene").gameObject;

        window.transform.Find("wen").GetChild(0).gameObject.SetActive(false);
        ODButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(false);
        TrainButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(false);
        StartprocessButton.transform.Find("Slider").gameObject.SetActive(false);
        window.gameObject.SetActive(false);
        loadSceneMask.SetActive(false);

        //openFileDialog.Filter = "All Files (*.*)|*.*";
        //openFileDialog.Multiselect = true;
    }

    void Update()
    {
        //string odfilespath = "";
        //string trainfilespath = "";
        //foreach(string odfilepath in ODfilesPath)
        //{
        //    odfilespath += odfilepath + " && ";
        //}
        //foreach (string trainfilepath in TrainfilesPath)
        //{
        //    trainfilespath += trainfilepath + " && ";
        //}
        //if (odfilespath != "")
        //{
        //    odfilespath = odfilespath.Substring(0, odfilespath.Length - 4);
        //}
        //if (trainfilespath != "")
        //{
        //    trainfilespath = trainfilespath.Substring(0, trainfilespath.Length - 4);
        //}
        //ODButton.transform.Find("path").GetComponent<Text>().text = odfilespath;
        //TrainButton.transform.Find("path").GetComponent<Text>().text = trainfilespath;

        if (processSlider.value == 1)
        {
            //if (processSlider.transform.Find("Text (Legacy)").GetComponent<Text>().text[0] == 'D')
            //{
                StartCoroutine(processSliderActiveFalse());
            //}
        }

        if (File.Exists(Preprocess.preprocess.OutODfilePath + "/ODweekday.josn") 
            && File.Exists(Preprocess.preprocess.OutODfilePath + "/ODweekend.josn")
            && File.Exists(Preprocess.preprocess.OutTrainfilePath + "/Trainweekday.josn")
            && File.Exists(Preprocess.preprocess.OutTrainfilePath + "/Trainweekend.josn"))
        {
            simlationButton.interactable = true;
        }
        else
        {
            simlationButton.interactable = false;
        }
    }

    IEnumerator processSliderActiveFalse()
    {
        yield return new WaitForSeconds(1f);
        processSlider.gameObject.SetActive(false);
        Preprocess.preprocess.processValue = 0;
        processValueChange("");
    }

    public void OpenFileExplorer(UnityEngine.UI.Button button)
    {
        //if (openFileDialog.ShowDialog() == DialogResult.OK)
        //{
        //    if (button == ODButton)
        //    {
        //        ODfilesPath = openFileDialog.FileNames;
        //    }
        //    else if (button == TrainButton)
        //    {
        //        TrainfilesPath = openFileDialog.FileNames;
        //    }
        //}
        打开窗口(0, button);
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            //processSlider.value = Mathf.Clamp01(asyncOperation.progress / 0.9f);//unity加载场景进度最大0.9
            //Debug.Log(processSlider.value);
            //processValueChange("Loading Scene ");

            yield return null;
        }

        loadSceneMask.SetActive(false);
    }

    private IEnumerator LoadSceneTextChange()
    {
        loadSceneMask.transform.GetChild(0).GetComponent<Text>().text = "Loading Scene.";
        yield return new WaitForSeconds(0.4f);
        loadSceneMask.transform.GetChild(0).GetComponent<Text>().text = "Loading Scene..";
        yield return new WaitForSeconds(0.4f);
        loadSceneMask.transform.GetChild(0).GetComponent<Text>().text = "Loading Scene...";
        yield return new WaitForSeconds(0.4f);
    }

    private void LoadSceneMask()
    {
        StartCoroutine(LoadSceneTextChange());
    }

    public void SimulationStart()
    {
        if (sceneName != "")
        {
            //Preprocess.preprocess.processValue = 0;
            //processValueChange("Loading Scene ");
            //processSlider.gameObject.SetActive(true);
            //StartCoroutine(LoadSceneAsync(sceneName));

            loadSceneMask.SetActive(true);
            //new Thread(LoadSceneMask).Start();
            SceneManager.LoadScene(sceneName);
        }
    }

    public void WindowStart(GameObject sceneButton)
    {
        window.gameObject.SetActive(true);
        sceneName = sceneButton.name;
        window.transform.Find("name").GetComponent<Text>().text = sceneButton.transform.GetChild(0).GetComponent<Text>().text;
    }

    public void WindowEnd()
    {
        window.gameObject.SetActive(false);
        sceneName = "";
    }

    public void WenStart()
    {
        window.transform.Find("wen").GetChild(0).gameObject.SetActive(true);
    }

    public void WenEnd()
    {
        window.transform.Find("wen").GetChild(0).gameObject.SetActive(false);
    }

    public void ODablemultiStart()
    {
        ODButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(true);
    }

    public void ODablemultiEnd()
    {
        ODButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(false);
    }

    public void TrainablemultiStart()
    {
        TrainButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(true);
    }

    public void TrainablemultiEnd()
    {
        TrainButton.transform.Find("ablemultichoice").Find("Button (Legacy)").gameObject.SetActive(false);
    }

    public async Task ProcessStart()
    {
        Preprocess.preprocess.processValue = 0;
        processValueChange("Data preprocessing ");
        processSlider.gameObject.SetActive(true);
        await Task.Run(() => Preprocess.preprocess.PreprocessRun());
    }
    public void ProcessStartwxl()
    {
        Preprocess.preprocess.processValue = 0;
        processValueChange("Data preprocessing ");
        processSlider.gameObject.SetActive(true);
        Task.Run(() => Preprocess.preprocess.PreprocessRun());
    }

    public void processValueChange(string prefix)
    {
        processSlider.value = Preprocess.preprocess.processValue;
        processSlider.transform.Find("Text (Legacy)").GetComponent<Text>().text = prefix + $"{(int)(processSlider.value * 100)}%";
    }


    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
    public static extern bool GetOpenFileName([In, Out] 对话框属性设置 对话框设置);
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
    public static extern bool GetSaveFileName([In, Out] 对话框属性设置 对话框设置);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class 对话框属性设置
    {
        public int 当前类占用内存数量;
        public IntPtr 开启对话框的主程序句柄;
        public IntPtr 对话框句柄;
        public string 文件筛选;
        public string 默认文件名;
        public int 最大筛选;
        public int 当前筛选种类序号;
        public string 文件全名 = new string(new char[256]);
        public int 文件全名最大长度 = 256;
        public string 文件名 = new string(new char[64]);
        public int 文件名最大长度 = 64;
        public string 默认文件目录;
        public string 对话框的标题;
        public 窗口开启模式 开启模式 = (窗口开启模式)(1 << 3);
        public short 文件偏移;
        public short 文件扩充;
        public string 缺陷;
        public IntPtr 数据指针;
        public IntPtr 钩子指针;
        public string 模板名称;
        public IntPtr 备用指针;
        public int 备用整数;
        public int EX标志;
    }
    [Flags]
    public enum 窗口开启模式
    {
        可多选 = 1 << 9,
        路径必须存在 = 1 << 11,
        文件必须存在 = 1 << 12,
    }
    public string[] 文件筛选目录;
    public string 文件全名;
    public string 文件名;
    public 窗口开启模式 开启模式;
    public enum 打开保存状态
    {
        打开文件 = 0, 保存文件 = 1
    }
    public void 打开窗口(int 对话框状态, Button 按钮)
    {
        打开窗口((打开保存状态)对话框状态, 按钮);
    }
    public void 打开窗口(打开保存状态 对话框状态, Button 按钮)
    {
        对话框属性设置 暂存 = new 对话框属性设置();
        暂存.当前类占用内存数量 = Marshal.SizeOf(暂存);

        暂存.文件筛选 = "";
        for (int fFor = 0; fFor < 文件筛选目录.Length; fFor++)
        {
            暂存.文件筛选 += 文件筛选目录[fFor] + "\0";
        }
        暂存.当前筛选种类序号 = 1;
        暂存.默认文件目录 = Directory.GetCurrentDirectory();
        暂存.对话框的标题 = 按钮.transform.Find("Text (Legacy)").GetComponent<Text>().text;
        if (按钮.transform.Find("ablemultichoice").GetComponent<Toggle>().isOn)
        {
            暂存.开启模式 = 窗口开启模式.可多选;
        }
        else
        {
            暂存.开启模式 |= 开启模式;
        }

        switch (对话框状态)
        {
            case 打开保存状态.保存文件:
                if (GetSaveFileName(暂存))
                {
                    文件全名 = 暂存.文件全名;
                    文件名 = 暂存.文件名;
                    StartCoroutine(读取图片(暂存));
                }
                else
                {
                    print("保存取消");
                }
                break;
            case 打开保存状态.打开文件:
                if (GetOpenFileName(暂存))
                {
                    文件全名 = 暂存.文件全名;
                    文件名 = 暂存.文件名;
                    //StartCoroutine(读取图片(暂存));
                    ChangePath(文件全名, 按钮);
                }
                else
                {
                    print("打开取消");
                }
                break;
        }
    }
    public RawImage 图片显示UI;
    IEnumerator 读取图片(对话框属性设置 文件)
    {
        WWW www = new WWW(文件.文件全名);
        if (www.error == null)
        {
            图片显示UI.texture = www.texture;
        }
        yield return www;
    }

    public void ChangePath(string inputString, Button button)
    {
        if (inputString != "")
        {
            string[] parts = inputString.Split("\\");
            string fileName = parts[parts.Length - 1];
            string[] partSpaces = fileName.Split(" ");
            string basePath = inputString.Substring(0, inputString.Length - fileName.Length).Trim();

            List<string> fullPathList = new List<string>();
            for (int i = 0; i < partSpaces.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(partSpaces[i]) && partSpaces[i] != "")
                {
                    fullPathList.Add(System.IO.Path.Combine(basePath, partSpaces[i].Trim()));
                }
            }

            string fileNameList = "";
            foreach (string partSpace in partSpaces)
            {
                if (!string.IsNullOrWhiteSpace(partSpace) && partSpace != "")
                {
                    fileNameList += partSpace + "\n";
                }
            }

            button.transform.Find("path").GetComponent<Text>().text = fileNameList;
            if (button == ODButton)
            {
                Preprocess.preprocess.ODfilesPath = fullPathList;
            }
            else if (button == TrainButton)
            {
                Preprocess.preprocess.TrainfilesPath = fullPathList;
            }
        }
    }
}
