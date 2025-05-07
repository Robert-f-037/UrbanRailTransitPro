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
        �򿪴���(0, button);
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncOperation.isDone)
        {
            //processSlider.value = Mathf.Clamp01(asyncOperation.progress / 0.9f);//unity���س����������0.9
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
    public static extern bool GetOpenFileName([In, Out] �Ի����������� �Ի�������);
    [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
    public static extern bool GetSaveFileName([In, Out] �Ի����������� �Ի�������);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class �Ի�����������
    {
        public int ��ǰ��ռ���ڴ�����;
        public IntPtr �����Ի������������;
        public IntPtr �Ի�����;
        public string �ļ�ɸѡ;
        public string Ĭ���ļ���;
        public int ���ɸѡ;
        public int ��ǰɸѡ�������;
        public string �ļ�ȫ�� = new string(new char[256]);
        public int �ļ�ȫ����󳤶� = 256;
        public string �ļ��� = new string(new char[64]);
        public int �ļ�����󳤶� = 64;
        public string Ĭ���ļ�Ŀ¼;
        public string �Ի���ı���;
        public ���ڿ���ģʽ ����ģʽ = (���ڿ���ģʽ)(1 << 3);
        public short �ļ�ƫ��;
        public short �ļ�����;
        public string ȱ��;
        public IntPtr ����ָ��;
        public IntPtr ����ָ��;
        public string ģ������;
        public IntPtr ����ָ��;
        public int ��������;
        public int EX��־;
    }
    [Flags]
    public enum ���ڿ���ģʽ
    {
        �ɶ�ѡ = 1 << 9,
        ·��������� = 1 << 11,
        �ļ�������� = 1 << 12,
    }
    public string[] �ļ�ɸѡĿ¼;
    public string �ļ�ȫ��;
    public string �ļ���;
    public ���ڿ���ģʽ ����ģʽ;
    public enum �򿪱���״̬
    {
        ���ļ� = 0, �����ļ� = 1
    }
    public void �򿪴���(int �Ի���״̬, Button ��ť)
    {
        �򿪴���((�򿪱���״̬)�Ի���״̬, ��ť);
    }
    public void �򿪴���(�򿪱���״̬ �Ի���״̬, Button ��ť)
    {
        �Ի����������� �ݴ� = new �Ի�����������();
        �ݴ�.��ǰ��ռ���ڴ����� = Marshal.SizeOf(�ݴ�);

        �ݴ�.�ļ�ɸѡ = "";
        for (int fFor = 0; fFor < �ļ�ɸѡĿ¼.Length; fFor++)
        {
            �ݴ�.�ļ�ɸѡ += �ļ�ɸѡĿ¼[fFor] + "\0";
        }
        �ݴ�.��ǰɸѡ������� = 1;
        �ݴ�.Ĭ���ļ�Ŀ¼ = Directory.GetCurrentDirectory();
        �ݴ�.�Ի���ı��� = ��ť.transform.Find("Text (Legacy)").GetComponent<Text>().text;
        if (��ť.transform.Find("ablemultichoice").GetComponent<Toggle>().isOn)
        {
            �ݴ�.����ģʽ = ���ڿ���ģʽ.�ɶ�ѡ;
        }
        else
        {
            �ݴ�.����ģʽ |= ����ģʽ;
        }

        switch (�Ի���״̬)
        {
            case �򿪱���״̬.�����ļ�:
                if (GetSaveFileName(�ݴ�))
                {
                    �ļ�ȫ�� = �ݴ�.�ļ�ȫ��;
                    �ļ��� = �ݴ�.�ļ���;
                    StartCoroutine(��ȡͼƬ(�ݴ�));
                }
                else
                {
                    print("����ȡ��");
                }
                break;
            case �򿪱���״̬.���ļ�:
                if (GetOpenFileName(�ݴ�))
                {
                    �ļ�ȫ�� = �ݴ�.�ļ�ȫ��;
                    �ļ��� = �ݴ�.�ļ���;
                    //StartCoroutine(��ȡͼƬ(�ݴ�));
                    ChangePath(�ļ�ȫ��, ��ť);
                }
                else
                {
                    print("��ȡ��");
                }
                break;
        }
    }
    public RawImage ͼƬ��ʾUI;
    IEnumerator ��ȡͼƬ(�Ի����������� �ļ�)
    {
        WWW www = new WWW(�ļ�.�ļ�ȫ��);
        if (www.error == null)
        {
            ͼƬ��ʾUI.texture = www.texture;
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
