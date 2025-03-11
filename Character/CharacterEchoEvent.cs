using UnityEngine;

public class CharacterEchoEvent : MonoBehaviour
{
    private MovePlayer playerCharacter;
    private GameObject playerCane;
    
    // ���� Ƚ�� ������ Count ������
    [SerializeField]
    private float endStopEchoTime = 2.0f;

    private int caneEchoCountMax = 1;
    private int caneEchoCount = 0;
    private int walkEchoCountMax = 1;
    private int walkEchoCount = 0;
    private int runEchoCountMax = 1;
    private int runEchoCount = 0;
    private float startStopEchoTime = 0;

    // �÷��̾�� �����̿� ���������� ���ؼ� ������Ʈ ������ ������
    private void Awake()
    {
        Transform[] allObjects = transform.GetComponentsInChildren<Transform>();

        foreach (Transform obj in allObjects)
        {
            if (obj.name == "cane")
            {
                playerCane = obj.gameObject;
                break;
            }
        }
        playerCharacter = transform.parent.GetComponent<MovePlayer>();
    }

    // �÷��̾ ���������� ���ڸ� �Ѹ��� ����
    private void Update()
    {
        if (playerCharacter.MoveDirection == Vector3.zero)
        {
            startStopEcoTime += Time.deltaTime;

            if (startStopEcoTime > endStopEcoTime)
            {
                startStopEcoTime = 0.0f;
                StopEvent();
            }
        }
        else
        {
            startStopEcoTime = 0.0f;
        }
    }

    // �÷��̾� �ʿ��� �̺�Ʈ�� ���޹޾� ���ڸ� ����
    public void StopEvent()
    {
        if (!playerCharacter.IsEco) return;

        playerCharacter.StopEco(playerCharacter.transform);
    }

    // ������ �Ҹ��� ���� Ƚ�� ����
    public async void CaneEvent()
    {
        if (!playerCharacter.IsEco) return;
        GameManager.Instance.SoundManager.PlaySound2D("Sound/caneSound", SoundType.SFX, 1);

        caneEcoCount = 0;

        while (caneEcoCount < caneEcoCountMax)
        {
            await UniTask.Delay(600);
            playerCharacter.CaneEco(playerCane.transform);
            caneEcoCount++;
        }
    }

    // ���� �� ���� Ƚ�� ����
    public async void WalkEvent()
    {
        if (!playerCharacter.IsEco) return;
     
        walkEcoCount = 0;

        while (walkEcoCount < walkEcoCountMax)
        {
            await UniTask.Delay(600);
            playerCharacter.WalkEco(playerCharacter.transform);
            walkEcoCount++;
        }
    }

    // �޸� �� ���� Ƚ�� ����
    public async void RunEvent()
    {
        if (!playerCharacter.IsEco) return;
       
        runEcoCount = 0;

        while (runEcoCount < runEcoCountMax)
        {
            await UniTask.Delay(800);
            playerCharacter.RunEco(playerCharacter.transform);
            runEcoCount++;
        }
    }

    // ���� �� �Ҹ� �̺�Ʈ
    public void WalkSoundEvent()
    {
        GameManager.Instance.SoundManager.PlaySound2D("Sound/walk", SoundType.SFX, 0.2f, 1.0f);
    }

    // �޸� �� �Ҹ� �̺�Ʈ
    public void RunSoundEvent()
    {
        GameManager.Instance.SoundManager.PlaySound2D("Sound/walk", SoundType.SFX, 0.2f, 1.0f);
    }
}
