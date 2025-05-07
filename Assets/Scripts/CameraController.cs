using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed;
    public float dragSpeed;
    public float maxZoom;
    public float minZoom;
    public float maxBoundX;
    public float minBoundX;
    public float maxBoundY;
    public float minBoundY;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    private GameObject trainPanel;
    private GameObject stationPanel;

    void Start()
    {
        trainPanel = GameManager.gameManager.trainPanel;
        stationPanel = GameManager.gameManager.stationPanel;
    }

    public bool mouseInBackGround(Vector3 mousePos, RectTransform bound)
    {
        Vector3[] corners = new Vector3[4];
        bound.GetWorldCorners(corners);
        if (mousePos.x > corners[0].x && mousePos.x < corners[2].x && mousePos.y > corners[0].y && mousePos.y < corners[2].y)
        {
            //Debug.Log("true");
            return true;
        }
        else
        {
            //Debug.Log("false");
            return false;
        }
    }

    void Update()
    {
        Ray ray = this.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (!mouseInBackGround(Input.mousePosition, GameManager.gameManager.backGround.GetComponent<RectTransform>()))
        {
            float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
            if (this.GetComponent<Camera>().orthographicSize >= maxZoom)
            {
                scrollWheel = (scrollWheel > 0 ? 0 : scrollWheel);
            }
            else if (this.GetComponent<Camera>().orthographicSize <= minZoom)
            {
                scrollWheel = (scrollWheel < 0 ? 0 : scrollWheel);
            }
            if (scrollWheel != 0)
            {
                this.GetComponent<Camera>().orthographicSize += scrollWheel * zoomSpeed * Time.deltaTime;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out hit) && hit.collider.transform.parent
                    && hit.collider.transform.parent.parent
                    && hit.collider.transform.parent.parent == GameManager.gameManager.GetComponent<Passengerflow>().stations.transform)
                {
                    GameManager.gameManager.stationChildClick = hit.collider.gameObject;
                    trainPanel.SetActive(false);
                    stationPanel.SetActive(true);
                }
                else if (Physics.Raycast(ray, out hit) && hit.collider.transform.parent
                    && hit.collider.transform.parent.parent
                    && hit.collider.transform.parent.parent == GameManager.gameManager.GetComponent<Passengerflow>().trains.transform)
                {
                    bool breakFlag = false;
                    foreach (Line line in GameManager.gameManager.GetComponent<Passengerflow>().lines.GetComponentsInChildren<Line>())
                    {
                        foreach (Train train in line.trains)
                        {
                            if (train.trainObject == hit.collider.transform.parent.gameObject)
                            {
                                GameManager.gameManager.trainClick = train;
                                breakFlag = true;
                                break;
                            }
                        }
                        if (breakFlag)
                        {
                            break;
                        }
                    }
                    stationPanel.SetActive(false);
                    trainPanel.SetActive(true);
                }
                else
                {
                    isDragging = true;
                    dragOrigin = Input.mousePosition;
                    trainPanel.SetActive(false);
                    stationPanel.SetActive(false);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
            else if (isDragging && dragOrigin.magnitude > 0f)
            {
                this.transform.position -= (Input.mousePosition - dragOrigin) * dragSpeed;
                dragOrigin = Input.mousePosition;
            }
        }

        float moveX = 0;
        float moveY = 0;
        if (this.transform.position.x + this.GetComponent<Camera>().orthographicSize * 1920/1080 > maxBoundX)
        {
            moveX = maxBoundX - this.GetComponent<Camera>().orthographicSize * 1920 / 1080 - this.transform.position.x;
        }
        else if (this.transform.position.x - this.GetComponent<Camera>().orthographicSize * 1920/1080 < minBoundX)
        {
            moveX = minBoundX + this.GetComponent<Camera>().orthographicSize * 1920 / 1080 - this.transform.position.x;
        }
        if (this.transform.position.y + this.GetComponent<Camera>().orthographicSize > maxBoundY)
        {
            moveY = maxBoundY - this.GetComponent<Camera>().orthographicSize - this.transform.position.y;
        }
        else if (this.transform.position.y - this.GetComponent<Camera>().orthographicSize < minBoundY)
        {
            moveY = minBoundY + this.GetComponent<Camera>().orthographicSize - this.transform.position.y;
        }
        this.transform.position += new Vector3(moveX, moveY, 0f);
    }
}
