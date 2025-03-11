using UnityEngine;

public class KeypadCollisionCheck : MonoBehaviour
{
    [SerializeField]
    private MovePlayer playerCharacter;
    [SerializeField]
    private FollowCamera followCamera;
    [SerializeField]
    private GameObject KeypadUI;

    // ó�� Ű�е�� ��ȣ�ۿ������� üũ��
    private bool oneCheckInteraction = false;

    public bool OneCheckInteraction
    {
        get { return oneCheckInteraciton; }
        set { oneCheckInteraciton = value; }
    }

    // Ű�е� �ȿ� Player�±װ� ������ �� �۵�
    // Ű�е�� ��ȣ�ۿ� ����, Ű�е�� UI �۵�
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!oneCheckInteraciton)
            {
                KeypadUI.SetActive(true);
            }
            else
            {
                KeypadUI.SetActive(false);
            }
          
            if (playerCharacter.IsInteraction)
            {
                oneCheckInteraciton = true;
                playerCharacter.IsKeyPadSight = true;
                followCamera.IsTargetKeypad = true;
                playerCharacter.VisibleMousePointer(true);
                playerCharacter.SavePlayerPos();
                playerCharacter.gameObject.transform.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    // �浹 ������ ������� UI��Ȱ��ȭ
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            KeypadUI.SetActive(false);
        }
    }
}
