using UnityEngine;
using UnityEngine.InputSystem;

namespace MyPlane
{
    [RequireComponent(typeof(Rigidbody))]
    public class AircraftController : MonoBehaviour
    {
        [SerializeField] private float maxThrust = 200000f;
        [SerializeField] private float throttleSensitivity = 1f;
        [SerializeField] private float pitchSensitivity = 2.5f;
        [SerializeField] private float rollSensitivity = 2.5f;
        [SerializeField] private float yawSensitivity = 0.25f;
        [SerializeField] private float baseLift = 120000f;
        [SerializeField] private float dragCoefficient = 0.2f;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform centerOfMass;
        
        private Rigidbody _rb;
        private InputAction _throttleAction;
        private InputAction _pitchAction;
        private InputAction _rollAction;
        private InputAction _yawAction;
        private float _currentThrottle = 0.7f;
        private Vector2 _mouseInput;
        private float _keyboardYaw;
        private Vector3 _lastVelocity;
        private float _speed;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            SetupRigidbody();
            SetupInputActions();
        }

        private void SetupRigidbody()
        {
            // Устанавливаем центр масс в центре самолета
            _rb.centerOfMass = centerOfMass.localPosition;

            _rb.mass = 10000f;
            _rb.linearDamping = 0f;
            _rb.angularDamping = 1.5f; // Уменьшаем для более отзывчивого управления
            _rb.useGravity = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Начальные условия
            _rb.linearVelocity = transform.forward * 100f;
            transform.position = new Vector3(0, 500, 0);
        }
        
        private void Update()
        {
            HandleThrottleInput();
        }

        private void HandleThrottleInput()
        {
            float throttleInput;
            if (_throttleAction?.ReadValue<float>() != null) throttleInput = _throttleAction.ReadValue<float>();
            else throttleInput = 0f;
            _currentThrottle = Mathf.Clamp01(_currentThrottle + throttleInput * throttleSensitivity * Time.deltaTime);
        }

        private void SetupInputActions()
        {
            if (!inputActions)
            {
                return;
            }

            var actionMap = inputActions.FindActionMap("AircraftControls");
            if (actionMap == null)
            {
                return;
            }

            _throttleAction = actionMap.FindAction("Throttle");
            _pitchAction = actionMap.FindAction("Pitch");
            _rollAction = actionMap.FindAction("Roll");
            _yawAction = actionMap.FindAction("Yaw");

            _pitchAction.performed += ctx => _mouseInput.y = ctx.ReadValue<float>();
            _pitchAction.canceled += ctx => _mouseInput.y = 0f;

            _rollAction.performed += ctx => _mouseInput.x = ctx.ReadValue<float>();
            _rollAction.canceled += ctx => _mouseInput.x = 0f;

            _yawAction.performed += ctx => _keyboardYaw = ctx.ReadValue<float>();
            _yawAction.canceled += ctx => _keyboardYaw = 0f;
        }

        private void OnEnable()
        {
            _throttleAction?.Enable();
            _pitchAction?.Enable();
            _rollAction?.Enable();
            _yawAction?.Enable();
        }

        private void OnDisable()
        {
            _throttleAction?.Disable();
            _pitchAction?.Disable();
            _rollAction?.Disable();
            _yawAction?.Disable();
        }

        private void FixedUpdate()
        {
            _speed = _rb.linearVelocity.magnitude;

            ApplyThrust();
            ApplyAerodynamicForces();
            ApplyFlightControls();
            ApplyVelocityAlignment();

            _lastVelocity = _rb.linearVelocity;
        }

        private void ApplyThrust()
        {
            float thrust = _currentThrottle * maxThrust;
            Vector3 thrustForce = transform.forward * thrust;
            _rb.AddForce(thrustForce, ForceMode.Force);
        }

        private void ApplyAerodynamicForces()
        {
            if (_speed < 1f) return;
            
            float liftForce = baseLift * _currentThrottle;
            _rb.AddForce(transform.up * liftForce, ForceMode.Force);
            
            float dragForce = _speed * _speed * dragCoefficient;
            Vector3 dragDirection = -_rb.linearVelocity.normalized;
            _rb.AddForce(dragDirection * dragForce, ForceMode.Force);

            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            Vector3 sideDrag = new Vector3(localVelocity.x, 0, 0);
            sideDrag = transform.TransformDirection(sideDrag);
            _rb.AddForce(-sideDrag * 5000f, ForceMode.Force);
        }

        private void ApplyFlightControls()
        {
            if (Mathf.Abs(_mouseInput.y) > 0.01f)
            {
                float pitchInput = -_mouseInput.y * 0.01f;
                Vector3 pitchTorque = transform.right * (pitchInput * pitchSensitivity);
                _rb.AddTorque(pitchTorque, ForceMode.VelocityChange);
            }
            
            if (Mathf.Abs(_mouseInput.x) > 0.01f)
            {
                float rollInput = _mouseInput.x * 0.01f;
                Vector3 rollTorque = transform.forward * (rollInput * rollSensitivity);
                _rb.AddTorque(rollTorque, ForceMode.VelocityChange);
            }
            
            if (Mathf.Abs(_keyboardYaw) > 0.01f)
            {
                Vector3 yawTorque = transform.up * (_keyboardYaw * yawSensitivity);
                _rb.AddTorque(yawTorque, ForceMode.VelocityChange);
            }
        }

        private void ApplyVelocityAlignment()
        {
            if (_speed > 10f)
            {
                Vector3 currentVelocityDir = _rb.linearVelocity.normalized;
                Vector3 targetVelocityDir = transform.forward;
                
                float alignmentStrength = 2f; // Сила выравнивания
                Vector3 velocityCorrection = (targetVelocityDir - currentVelocityDir) * alignmentStrength * _speed;
                
                _rb.AddForce(velocityCorrection * _rb.mass, ForceMode.Force);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 500, 700));

            GUI.color = Color.white;
            GUILayout.Label("=== AIRCRAFT PHYSICS DEBUG ===");
            
            float speedChange = _speed - _lastVelocity.magnitude;
            GUI.color = speedChange > 0 ? Color.green : Color.red;
            GUILayout.Label($"Speed: {_speed:0.0} m/s (Δ: {speedChange:0.0})");

            GUI.color = Color.green;
            GUILayout.Label($"Altitude: {transform.position.y:0.0} m");
            GUILayout.Label($"Throttle: {_currentThrottle * 100:0}%");
            
            Vector3 localVel = transform.InverseTransformDirection(_rb.linearVelocity);
            GUI.color = Color.yellow;
            GUILayout.Label($"Local Velocity:");
            GUILayout.Label($"  Forward: {localVel.z:0.0}");
            GUILayout.Label($"  Right: {localVel.x:0.0}");
            GUILayout.Label($"  Up: {localVel.y:0.0}");
            
            Vector3 velocityDir = _rb.linearVelocity.normalized;
            Vector3 forwardDir = transform.forward;
            float alignment = Vector3.Dot(velocityDir, forwardDir);

            GUILayout.Label($"Velocity Alignment: {alignment:0.00}");
            GUILayout.Label($"Forward Dir: {forwardDir}");
            GUILayout.Label($"Velocity Dir: {velocityDir}");
            
            GUILayout.Label($"Mouse Input: X:{_mouseInput.x:0.00} Y:{_mouseInput.y:0.00}");
            GUILayout.Label($"Yaw Input: {_keyboardYaw:0.00}");
            
            GUILayout.Label($"Pitch: {transform.eulerAngles.x:0.0}°");
            GUILayout.Label($"Roll: {transform.eulerAngles.z:0.0}°");
            GUILayout.Label($"Yaw: {transform.eulerAngles.y:0.0}°");
            
            GUI.color = Color.white;
            GUILayout.Label("=== CONTROLS ===");
            GUILayout.Label("W/S: Throttle");
            GUILayout.Label("Mouse: Pitch/Roll");
            GUILayout.Label("A/D: Yaw");
            
            GUILayout.Label($"Angular Velocity: {_rb.angularVelocity.magnitude:0.00}");

            GUILayout.EndArea();
        }
    }
}



/*

Awake()
- Инициализация компонентов при создании объекта
- Вызывает настройку Rigidbody и системы ввода

SetupRigidbody()
- Настраивает физические параметры самолета:
  - Масса, сопротивление, гравитация
  - Центр масс
  - Начальная скорость и позиция

SetupInputActions()
- Находит и настраивает Actions из Input System
- Подписывается на события ввода (мышь, клавиатура)

OnEnable()/OnDisable()
- Включает/выключает обработку ввода при активации объекта

Update()
- Вызывается каждый кадр
- Обрабатывает плавное изменение газа (Throttle)

FixedUpdate()
- Вызывается с фиксированным интервалом для физики
- Основной цикл применения сил и управления

HandleThrottleInput()
- Читает ввод W/S и плавно изменяет значение газа (0-1)

ApplyThrust()
- Создает силу тяги вперед по направлению самолета
- Зависит от текущего значения газа

ApplyAerodynamicForces()
- Подъемная сила - вверх относительно самолета
- Сопротивление - против движения  
- Боковое сопротивление - убирает боковое скольжение

ApplyFlightControls()
- Преобразует ввод мыши/клавиш в вращающие моменты:
  - Pitch - вращение вокруг оси X (нос вверх/вниз)
  - Roll - вращение вокруг оси Z (крен)
  - Yaw - вращение вокруг оси Y (повороты)

ApplyVelocityAlignment()
- Ключевой метод - выравнивает вектор скорости с направлением самолета
- Создает реалистичную аэродинамику (самолет поворачивает, а не скользит)

ApplyStability()
- Гасит излишнее вращение для стабильного полета

OnGUI()
- Отображает телеметрию и отладочную информацию

Основной принцип:
Ввод -> Вращение -> Выравнивание скорости -> Реалистичный полет
 
*/