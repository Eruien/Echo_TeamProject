using UnityEngine;
using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{
    // 사용자 정의, 유니티 클래스

    // 키패드 비밀번호 리셋 용
    [SerializeField]
    private GameObject keypadDark;
    // F키 UI 활성, 비활성 용
    [SerializeField]
    private GameObject startDoorUI;
    [SerializeField]
    private GameObject endDoorUI;
    // 달리기 버튼과 연결 용도
    [SerializeField]
    private RunButton runButton;

    private Keypad keypad;
    private Camera characterCamera;
    private Rigidbody rigidBody;
    private Animator characterAnimation;
    private FollowCamera followCamera;
    // PC용 NewInputSystem
    private PlayerNewInput playerNewInput;
    // Mobile용 NewInputSystem
    private PlayerInputMobile playerInputMobile;
    // 달리기 이벤트 함수 등록 용
    private InputAction runAction;
    // Mobile용 카메라 회전 
    private InputAction lookLeftAction;
    // Mobile용 카메라 회전 
    private InputAction lookRightAction;
    // 버튼 클릭할 때 소리 한 번만 나게
    private InteractObject interactObject;
    // 키패드랑 충돌 났는지 체크 용
    private KeypadCollisionCheck keypadCollisionCheck;

    public event PropertyChangedEventHandler PropertyChanged;

    // 스태미너 UI 인스펙터 등록
    public StaminaUI staminaUI;

    // Vector나 기본 자료형
    private Vector3 moveDirection = Vector3.zero; // 현재 방향 벡터
    public Vector3 MoveDirection
    {
        get { return moveDirection; }
        set { moveDirection = value; }
    }

    [SerializeField]
    private Vector3 rayUp = Vector3.zero; // 현재 캐릭터 위치에서 살짝 위에 포지션을 주기 위해
    private Vector2 keyboardInput = Vector2.zero; // 키보드 + -
    private Vector3 moveForward = Vector3.zero; // 키보드 위, 아래 판정용
    private Vector3 moveRight = Vector3.zero; // 키보드 왼, 오 판정용
    private Vector3 prevPlayerPos = Vector3.zero; // 키보드 왼, 오 판정용

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
    private float mouseSensitivity = 15.0f; // 마우스 감도

    public float MouseSensitivity
    {
        get { return mouseSensitivity; }
        set { mouseSensitivity = value; }
    }

    [SerializeField]
    private bool isEcho = true; // 에코가 켜져있는지 여부

    public bool IsEcho
    {
        get { return isEcho; }
        set { isEcho = value; }
    }

    // true일 경우 캐릭터의 움직임이 멈추게
    private bool characterStop = false;

    public bool CharacterStop
    {
        get { return characterStop; }
        set { characterStop = value; }
    }

    // 키패드를 보고 있는 시야인지
    private bool isKeyPadSight = false;

    public bool IsKeyPadSight
    {
        get { return isKeyPadSight; }
        set { isKeyPadSight = value; }
    }

    private bool isInteraction = false; // 키패드를 확대시키고 있는지 여부

    public bool IsInteraction
    {
        get { return isInteraction; }
        set { isInteraction = value; }
    }

    [SerializeField]
    private float defaultSpeed = 5.0f; // 초기 스피드
    [SerializeField]
    private float moveSpeed = 5.0f; // 현재 움직이는 속도
    [SerializeField]
    private float runSpeed = 7.0f; // 달리기 속도
    [SerializeField]
    private float consumeStaminaRate = 20.0f; // 스태미나 소비 비율
    [SerializeField]
    private float recoveryStaminaRate = 10.0f; // 스태미나 회복 비율
    [SerializeField]
    private float fullStamina = 200.0f; // 총 스태미나

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

    // NewInputSystem 과 모바일용 이벤트 등록
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

    // 위의 이벤트 해제
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

        // 각종 행동 취소
        if (IsEscape)
        {
            IsEscape = false;
            characterStop = false;
            followCamera.IsTargetKeypad = false;
            VisibleMousePointer(false);
        }

        // 스태미너 계산
        //staminaUI.SetValue(currentStamina / fullStamina);
    }

    // 각종 물리 계산(벽 체크, 움직임, 모바일용 카메라)
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

        // 실시간 Run 체크

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

        // PC버전 달리기 로직 주석
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

        // 애니메이션이 전부 끝났을 때를 체크 끝났을 때 false로 돌려주어 다시 반복되게
        if (characterAnimation.GetCurrentAnimatorStateInfo(1).IsName("RightHand Layer.Stamping")
       && characterAnimation.GetCurrentAnimatorStateInfo(1).normalizedTime >= 0.9f)
        {
            characterAnimation.SetBool("IsStamping", false);
        }
    }

    // 사용자 정의 함수
    // 공개용

    // 스태미나 초기화
    public void InitStamina()
    {
        CurrentStamina = fullStamina;
    }

    // 플레이어 위치 저장용
    public void SavePlayerPos()
    {
        prevPlayerPos = transform.position;
    }

    // 플레이어 위치 불러오기용
    public Vector3 LoadPlayerPos()
    {
        return prevPlayerPos;
    }

    // 이벤트 영상이 끝난다음 포지션 원래대로 되돌리기
    public void MousePosFront()
    {
        MouseX = 90;
        MouseY = 0;
    }

    // 에코 사용여부 판정
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

    // 마우스 포인터 키패드때는 보이게 아닐땐 감추기
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

    // 에코를 멈추는 메서드
    public void StopEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // 걸을 때 에코를 쏘는 메서드
    public void WalkEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // 달릴 때 에코를 쏘는 메서드
    public void RunEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerRunEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // 지팡이와 관련되서 에코를 쏘는 메서드
    public void CaneEcho(Transform EchoPos)
    {
        var t1 = GameManager.Instance.ObjectPool.Get(PoolingType.PlayerEcho);
        t1.Activate(EchoPos);
        GameManager.Instance.ObjectPool.Return(t1);
    }

    // 비공개

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

    // 벽에 닿았는지 판정
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

    // 걸을때와 달릴 때 전환용 메서드
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

    // 스태미나 전체적으로 관리
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

    // 움직임 전체적으로 관리
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

    // 모바일때 화면 설정
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

    // 카메라 좌, 우 버튼 입력이 들어왔을 때 작동하는 함수 좌 - 우 +
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

        characterCamera.transform.rotation = Quaternion.Euler(MouseY, MouseX, 0.0f);// 각 축을 한꺼번에 계산
        transform.rotation = Quaternion.Euler(0.0f, MouseX, 0.0f);
    }

    // 이벤트 바인딩 함수들
    public void OnMove(InputAction.CallbackContext context)
    {
        keyboardInput = context.ReadValue<Vector2>();
    }

    // 버튼 아래쪽으로 눌렀을 때 한 번 입력 받는 용도
    // 이벤트 등록용도로만 사용
    public void OnLookLeft(InputAction.CallbackContext context)
    {

    }

    // 버튼 아래쪽으로 눌렀을 때 한 번 입력 받는 용도
    // 이벤트 등록용도로만 사용
    public void OnLookRight(InputAction.CallbackContext context)
    {

    }

    // PC용 마우스 입력 받을시 카메라 회전
    public void OnMouse(InputAction.CallbackContext context)
    {
        if (characterStop) return;

        Vector2 mousePosition = context.ReadValue<Vector2>();

        float trunPlayer = mousePosition.x * mouseSensitivity * Time.deltaTime;
        MouseX += trunPlayer;

        if (MouseX > 360) MouseX -= 360.0f;
        if (MouseX < 0) MouseX += 360.0f;

        MouseY -= mousePosition.y * mouseSensitivity * Time.deltaTime;
        MouseY = Mathf.Clamp(MouseY, -90f, 50f); //Clamp를 통해 최소값 최대값을 넘지 않도록함

        characterCamera.transform.rotation = Quaternion.Euler(MouseY, MouseX, 0.0f);// 각 축을 한꺼번에 계산
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

    // 행동 취소용 이벤트 함수 취소할 행동이 많지 않아서 주석
    /* public void OnEscape(InputAction.CallbackContext context)
     {
         IsEscape = context.performed;
     }*/

    // 바닥을 찍을때 애니메이션 판정
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
