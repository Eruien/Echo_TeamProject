using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    // ī�޶�� �÷��̾� �Ӹ��� �������� ����ٴ�
    // Ű�е带 ������� �ϴ� �뵵�� �ֱ� ������ Ű�е�� ������ ����
    [SerializeField]
    private GameObject targetPlayer;
    [SerializeField]
    private GameObject targetKeypad;
    [SerializeField]
    private Vector3 keypadOffset;

    private MovePlayer playerCharacter;
    private GameObject playerHead;
    private GameObject target;
    private Camera cameraComponent;

    private bool isTargetKeypad;

    public bool IsTargetKeypad
    {
        get { return isTargetKeypad; }
        set { isTargetKeypad = value; }
    }

    // ī�޶� �þ߰� ������ ������Ƽ
    [SerializeField]
    private float cameraFOV = 60.0f;

    public float CameraFOV
    {
        get { return cameraFOV; }
        set { cameraFOV = value; }
    }

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        cameraComponent.fieldOfView = CameraFOV;
        playerCharacter = targetPlayer.GetComponent<MovePlayer>();
        FindChildBone(playerCharacter.transform, "head");
        target = targetPlayer;
        playerCharacter.VisibleMousePointer(true);
    }

    // Ű�е尡 Ÿ���� �Ǿ������� �÷��̾ Ÿ���� �Ǿ������� �и�
    void Update()
    {
        // ī�޶� FOV SET�Ǹ� ����
        cameraComponent.fieldOfView = CameraFOV;

        if (isTargetKeypad)
        {
            target = targetKeypad;
            transform.position = target.transform.position + target.transform.forward * keypadOffset.x;
            transform.LookAt(target.transform);
        }
        else
        {
            target = targetPlayer;
            transform.position = playerHead.transform.position;
        }
    }

    // �������� �ڽ��� �������� ��� ã�� ��
    void FindChildBone(Transform node, string boneName)
    {
        if (node.name == boneName)
        {
            playerHead = node.gameObject;
           
            return;
        }

        foreach (Transform child in node)
        {
            FindChildBone(child, boneName);
        }
    }
}
