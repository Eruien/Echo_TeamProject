using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    // ����� ����, ����Ƽ Ŭ����

    // Ű�е� ��й�ȣ ���� ��
    [SerializeField]
    private GameObject keypadDark;
    // FŰ UI Ȱ��, ��Ȱ�� ��
    [SerializeField]
    private GameObject startDoorUI;
    [SerializeField]
    private GameObject endDoorUI;
    // �޸��� ��ư�� ���� �뵵
    [SerializeField]
    private RunButton runButton;

    private Keypad keypad;
    private Camera characterCamera;
    private Rigidbody rigidBody;
    private Animator characterAnimation;
    private FollowCamera followCamera;
    // PC�� NewInputSystem
    private PlayerNewInput playerNewInput;
    // Mobile�� NewInputSystem
    private PlayerInputMobile playerInputMobile;
    // �޸��� �̺�Ʈ �Լ� ��� ��
    private InputAction runAction;
    // Mobile�� ī�޶� ȸ�� 
    private InputAction lookLeftAction;
    // Mobile�� ī�޶� ȸ�� 
    private InputAction lookRightAction;
    // ��ư Ŭ���� �� �Ҹ� �� ���� ����
    private InteractObject interactObject;
    // Ű�е�� �浹 ������ üũ ��
    private KeypadCollisionCheck keypadCollisionCheck;

    public event PropertyChangedEventHandler PropertyChanged;

    // ���¹̳� UI �ν����� ���
    public StaminaUI staminaUI;

    // Vector�� �⺻ �ڷ���
    private Vector3 moveDirection = Vector3.zero; // ���� ���� ����
    public Vector3 MoveDirection
    {
        get { return moveDirection; }
        set { moveDirection = value; }
    }

    [SerializeField]
    private Vector3 rayUp = Vector3.zero; // ���� ĳ���� ��ġ���� ��¦ ���� �������� �ֱ� ����
    private Vector2 keyboardInput = Vector2.zero; // Ű���� + -
    private Vector3 moveForward = Vector3.zero; // Ű���� ��, �Ʒ� ������
    private Vector3 moveRight = Vector3.zero; // Ű���� ��, �� ������
    private Vector3 prevPlayerPos = Vector3.zero; // Ű���� ��, �� ������

    [SerializeField]
    private float currentStamina = 0.0f;

    public float CurrentStamina
    {
        set
        {
            currentStamina = value;
            OnPropertyChanged("CurrentStamina");
        }
        get { return currentStamina / fullStamina; }
    }

    [SerializeField]
    private float mouseSensitivity = 15.0f; // ���콺 ����

    public float MouseSensitivity
    {
        get { return mouseSensitivity; }
        set { mouseSensitivity = value; }
    }

    [SerializeField]
    private bool isEcho = true; // ���ڰ� �����ִ��� ����

    public bool IsEcho
    {
        get { return isEcho; }
        set { isEcho = value; }
    }

    // true�� ��� ĳ������ �������� ���߰�
    private bool characterStop = false;

    public bool CharacterStop
    {
        get { return characterStop; }
        set { characterStop = value; }
    }

    // Ű�е带 ���� �ִ� �þ�����
    private bool isKeyPadSight = false;

    public bool IsKeyPadSight
    {
        get { return isKeyPadSight; }
        set { isKeyPadSight = value; }
    }

    private bool isInteraction = false; // Ű�е带 Ȯ���Ű�� �ִ��� ����

    public bool IsInteraction
    {
        get { return isInteraction; }
        set { isInteraction = value; }
    }

    [SerializeField]
    private float defaultSpeed = 5.0f; // �ʱ� ���ǵ�
    [SerializeField]
    private float moveSpeed = 5.0f; // ���� �����̴� �ӵ�
    [SerializeField]
    private float runSpeed = 7.0f; // �޸��� �ӵ�
    [SerializeField]
    private float consumeStaminaRate = 20.0f; // ���¹̳� �Һ� ����
    [SerializeField]
    private float recoveryStaminaRate = 10.0f; // ���¹̳� ȸ�� ����
    [SerializeField]
    private float fullStamina = 200.0f; // �� ���¹̳�

    private float MouseX = 0.0f;
    private float MouseY = 0.0f;
    private float time = 0.0f;
    private bool IsWalk = false;
    private bool IsEscape = false;
    private bool IsBorder = false;
    private bool IsStaminaZero = false;
    private bool IsUI = false;
    private bool IsRun = false;

    private void Awake()
    {
        playerNewInput = new PlayerNewInput();
        playerInputMoblie = new PlayerInputMobile();
        TryGetComponent(out rigidBody);
        characterAnimation = GetComponentInChildren<Animator>();
        characterCamera = Camera.main;
        followCamera = characterCamera.GetComponent<FollowCamera>();
        keypad = keypadDark.GetComponent<Keypad>();
        interactObject = GetComponent<InteractObject>();
        keypadCollisionCheck = keypad.GetComponent<KeypadCollisionCheck>();
    }

    // NewInputSystem �� ����Ͽ� �̺�Ʈ ���
    private void OnEnable()
    {
        runAction = playerNewInput.PlayerActions.Run;
        runAction.Enable();
        runAction.performed += OnRun;

        lookLeftAction = playerInputMoblie.PlayerAction.LookLeft;
        lookLeftAction.Enable();
        lookLeftAction.performed += OnLookLeft;

        lookRightAction = playerInputMoblie.PlayerAction.LookRight;
        lookRightAction.Enable();
        lookRightAction.performed += OnLookRight;
    }

    // ���� �̺�Ʈ ����
    private void OnDisable()
    {
        runAction.Disable();
        lookLeftAction.Disable();
        lookRightAction.Disable();
    }

    private void Start()
    {
        CurrentStamina = fullStamina;
        MouseX = transform.localEulerAngles.y;
    }

    private void Update()
    {
        PauseUI();

        // ���� �ൿ ���
        if (IsEscape)
        {
            IsEscape = false;
            characterStop = false;
            followCamera.IsTargetKeypad = false;
            VisibleMousePointer(false);
        }

        // ���¹̳� ���
        //staminaUI.SetValue(currentStamina / fullStamina);
    }

    // ���� ���� ���(�� üũ, ������, ����Ͽ� ī�޶�)
    private void FixedUpdate()
    {
        if (characterStop) return;

        moveDirection = Vector3.zero;
        time += Time.deltaTime;

        StopToWall();
        UICheck();
        Move();
        MoblieCameraLook();

        characterAnimation.SetBool("IsWalk", moveDirection != Vector3.zero);
        IsWalk = characterAnimation.GetBool("IsWalk");

        if (IsWalk)
        {
            characterAnimation.SetBool("IsStamping", false);
        }

        // �ǽð� Run üũ

        if (IsRun)
        {
            if (keyboardInput.y >= 0)
            {
                if (moveDirection != Vector3.zero)
                {
                    WalkRun(true);
                    StaminaSystem(true);
                    staminaUI.gameObject.SetActive(true);
                    GameManager.Instance.PlayerPresenter.progressUIView = staminaUI;
                }
            }

            if (keyboardInput.y < 0)
            {
                WalkRun(false);
            }
        }
        else
        {
            StaminaSystem(false);
            staminaUI.gameObject.SetActive(false);
            GameManager.Instance.PlayerPresenter.progressUIView = null;
        }

        // PC���� �޸��� ���� �ּ�
        /*   if (runAction.ReadValue<float>() > 0)
           {
               if (keyboardInput.y >= 0)
               {
                   if (moveDirection != Vector3.zero)
                   {
                       WalkRun(true);
                       StaminaSystem(true);
                       staminaUI.gameObject.SetActive(true);
                       GameManager.Instance.PlayerPresenter.progressUIView = staminaUI;
                   }
               }

               if (keyboardInput.y < 0)
               {
                   WalkRun(false);
               }
           }
           else
           {
               StaminaSystem(false);
               staminaUI.gameObject.SetActive(false);
               GameManager.Instance.PlayerPresenter.progressUIView = null;
           }*/

        // �ִϸ��̼��� ���� ������ ���� üũ ������ �� false�� �����־� �ٽ� �ݺ��ǰ�
        if (characterAnimation.GetCurrentAnimatorStateInfo(1).IsName("RightHand Layer.Stamping")
       && characterAnimation.GetCurrentAnimatorStateInfo(1).normalizedTime >= 0.9f)
        {
            characterAnimation.SetBool("IsStamping", false);
        }
    }

    // ����� ���� �Լ�
    // ������

    // ���¹̳� �ʱ�ȭ
    public void InitStamina()
    {
        CurrentStamina = fullStamina;
    }

    // �÷��̾� ��ġ �����
    public void SavePlayerPos()
    {
        prevPlayerPos = transform.position;
    }

    // �÷��̾� ��ġ �ҷ������
    public Vector3 LoadPlayerPos()
    {
        return prevPlayerPos;
    }

    // �̺�Ʈ ������ �������� ������ ������� �ǵ�����
    public void MousePosFront()
    {
        MouseX = 90;
        MouseY = 0;
    }

    // ���� ��뿩�� ����
    public void UseEcho(bool use)
    {
        if (use)
        {
            IsEcho = true;
        }
        else
        {
            IsEcho = false;
        }
    }

    // ���콺 ������ Ű�е嶧�� ���̰� �ƴҶ� ���߱�
    public void VisibleMousePointer(bool visible)
    {
        if (visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ���ڸ� ���ߴ� �޼���
    public void StopEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // ���� �� ���ڸ� ��� �޼���
    public void WalkEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // �޸� �� ���ڸ� ��� �޼���
    public void RunEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerRunEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // �����̿� ���õǼ� ���ڸ� ��� �޼���
    public void CaneEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // �����

    private void PauseUI()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && Time.timeScale == 1)
        {
            GameManager.Instance.UIManager.ShowUI<PauseUI>("UI/Pause UI");
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && Time.timeScale == 0)
        {
            GameManager.Instance.UIManager.CloaseCurrentUI();
        }
    }

    // ���� ��Ҵ��� ����
    private void StopToWall()
    {
        if (Physics.Raycast(transform.position + rayUp, transform.forward, 2.5f, LayerMask.GetMask("Wall")) ||
             Physics.Raycast(transform.position + rayUp, transform.right, 1.0f, LayerMask.GetMask("Wall")) ||
           Physics.Raycast(transform.position + rayUp, -transform.right, 1.0f, LayerMask.GetMask("Wall")))
        {
            IsBorder = true;
        }
        else
        {
            IsBorder = false;
        }
    }

    private void UICheck()
    {
        IsUI = Physics.Raycast(transform.position + rayUp, transform.forward, 5.0f, LayerMask.GetMask("UI"));

        if (IsUI)
        {
            startDoorUI.SetActive(true);
            endDoorUI.SetActive(true);
        }
        else
        {
            startDoorUI.SetActive(false);
            endDoorUI.SetActive(false);
        }
    }

    // �������� �޸� �� ��ȯ�� �޼���
    private void WalkRun(bool run)
    {
        if (run)
        {
            moveSpeed = runSpeed;
            characterAnimation.SetBool("IsRun", true);
            return;
        }
        else
        {
            moveSpeed = defaultSpeed;
            characterAnimation.SetBool("IsRun", false);
        }
    }

    // ���¹̳� ��ü������ ����
    private void StaminaSystem(bool run)
    {
        if (IsStaminaZero)
        {
            float maxStamina = currentStamina + (rEchoveryStaminaRate * Time.deltaTime);
            CurrentStamina = Mathf.Min(maxStamina, fullStamina);
            WalkRun(false);

            if (currentStamina >= 20) IsStaminaZero = false;
            return;
        }

        if (run)
        {
            float minStamina = currentStamina - (cousumeStaminaRate * Time.deltaTime);
            CurrentStamina = Mathf.Max(minStamina, 0);
        }
        else
        {
            float maxStamina = currentStamina + (rEchoveryStaminaRate * Time.deltaTime);
            CurrentStamina = Mathf.Min(maxStamina, fullStamina);
        }

        if (currentStamina <= 1)
        {
            IsStaminaZero = true;
        }
    }

    // ������ ��ü������ ����
    private void Move()
    {
        moveForward = keyboardInput.y * transform.forward;
        moveRight = keyboardInput.x * transform.right;


        if (IsBorder && moveSpeed > defaultSpeed)
        {
            moveSpeed = defaultSpeed;
        }

        moveDirection = moveForward + moveRight;
        moveDirection.Normalize();

        rigidBody.MovePosition(this.gameObject.transform.position + moveDirection * moveSpeed * Time.deltaTime);
    }

    // ����϶� ȭ�� ����
    private void MobileCameraLook()
    {
        if (lookLeftAction.ReadValue<float>() > 0)
        {
            Look(false, lookLeftAction);
        }
        else if (lookRightAction.ReadValue<float>() > 0)
        {
            Look(true, lookRightAction);
        }
    }

    // ī�޶� ��, �� ��ư �Է��� ������ �� �۵��ϴ� �Լ� �� - �� +
    private void Look(bool direction, InputAction action)
    {
        if (characterStop) return;

        float lookPos = action.ReadValue<float>();

        float turnPlayer = lookPos * mouseSensitivity * Time.deltaTime;

        if (direction)
        {
            MouseX += trunPlayer;
        }
        else
        {
            MouseX -= trunPlayer;
        }

        if (MouseX > 360) MouseX -= 360.0f;
        if (MouseX < 0) MouseX += 360.0f;

        characterCamera.transform.rotation = Quaternion.Euler(MouseY, MouseX, 0.0f);// �� ���� �Ѳ����� ���
        transform.rotation = Quaternion.Euler(0.0f, MouseX, 0.0f);
    }

    // �̺�Ʈ ���ε� �Լ���
    public void OnMove(InputAction.CallbackContext context)
    {
        keyboardInput = context.ReadValue<Vector2>();
    }

    // ��ư �Ʒ������� ������ �� �� �� �Է� �޴� �뵵
    // �̺�Ʈ ��Ͽ뵵�θ� ���
    public void OnLookLeft(InputAction.CallbackContext context)
    {

    }

    // ��ư �Ʒ������� ������ �� �� �� �Է� �޴� �뵵
    // �̺�Ʈ ��Ͽ뵵�θ� ���
    public void OnLookRight(InputAction.CallbackContext context)
    {

    }

    // PC�� ���콺 �Է� ������ ī�޶� ȸ��
    public void OnMouse(InputAction.CallbackContext context)
    {
        if (characterStop) return;

        Vector2 mousePosition = context.ReadValue<Vector2>();

        float trunPlayer = mousePosition.x * mouseSensitivity * Time.deltaTime;
        MouseX += trunPlayer;

        if (MouseX > 360) MouseX -= 360.0f;
        if (MouseX < 0) MouseX += 360.0f;

        MouseY -= mousePosition.y * mouseSensitivity * Time.deltaTime;
        MouseY = Mathf.Clamp(MouseY, -90f, 50f); //Clamp�� ���� �ּҰ� �ִ밪�� ���� �ʵ�����

        characterCamera.transform.rotation = Quaternion.Euler(MouseY, MouseX, 0.0f);// �� ���� �Ѳ����� ���
        transform.rotation = Quaternion.Euler(0.0f, MouseX, 0.0f);
    }

    public void OnRunMobile(InputAction.CallbackContext context)
    {
        if (IsRun && context.performed)
        {
            IsRun = false;
            runButton.isRun(false);
            return;
        }

        if (context.performed)
        {
            IsRun = true;
            runButton.isRun(true);
            return;
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (keyboardInput.y >= 0)
        {
            WalkRun(context.performed);
        }
    }

    public void OnInteraction(InputAction.CallbackContext context)
    {
        if (isKeyPadSight && context.performed)
        {
            keypadCollisionCheck.OneCheckInteraciton = false;
            isKeyPadSight = false;
            IsEscape = true;

            gameObject.transform.GetChild(0).gameObject.SetActive(true);

            keypad.ResetUserInput();
            return;
        }
        else
        {
            IsInteraction = context.performed;
        }

        if (context.performed)
        {
            StartCoroutine(interactObject.ClickedButton());
        }

    }

    // �ൿ ��ҿ� �̺�Ʈ �Լ� ����� �ൿ�� ���� �ʾƼ� �ּ�
    /* public void OnEscape(InputAction.CallbackContext context)
     {
         IsEscape = context.performed;
     }*/

    // �ٴ��� ������ �ִϸ��̼� ����
    public void OnStamping(InputAction.CallbackContext context)
    {
        if (IsWalk) return;
        if (characterStop) return;

        if (context.performed)
        {
            characterAnimation.SetBool("IsStamping", true);
            return;
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
