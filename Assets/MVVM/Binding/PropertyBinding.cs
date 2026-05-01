using System;
using System.ComponentModel;
using System.Reflection;
using Core.Architecture;
using Core.DI;
using MVVM.Binding.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Component = UnityEngine.Component;
using Object = UnityEngine.Object;

namespace MVVM.Binding
{
    public class PropertyBinding:StrictLifecycleMonoBehaviour,IPropertyBinding
    {
        [Header("绑定配置")]
        [SerializeField] private BindingMode _mode = BindingMode.OneWay;
        [SerializeField] private string _sourceProperty;
        [SerializeField] private string _targetProperty;

        [Header("数据源")]
        [SerializeField] private Object _source;
        [SerializeField] private Component _targetComponent;

        [Header("DI绑定（与_source二选一）")]
        [Tooltip("纯C# ViewModel的类型全名，如: MVVM.ViewModel.TestViewModel, Assembly-CSharp")]
        [SerializeField] private string _sourceTypeName;

        [Header("转换器")]
        [SerializeField] private IValueConverter _converter;

        [Inject] private IScopeProvider _scopeProvider;
        [Inject] private IServiceProvider _serviceProvider;

        // 反射缓存
        private PropertyInfo _sourcePropertyInfo;
        private PropertyInfo _targetPropertyInfo;
        private INotifyPropertyChanged _notifySource;
        
        // 在类字段区域添加
        private bool? _validationResult = null;
        private object _diSource;              // DI解析的纯C# ViewModel

        private object GetActualSource() => _diSource ?? (object)_source;

        // IPropertyBinding接口实现
        public string SourceProperty => _sourceProperty;
        public string TargetProperty => _targetProperty;
        public BindingMode BindingMode => _mode;
        public IValueConverter Converter => _converter;

        protected override void OnInitialize()
        {
            ValidateBindings();
        }
        protected override void OnStartExternal()
        {
            if (_validationResult!=null&&_validationResult.Value)  // 使用缓存结果
            {
                Bind();
                Debug.Log($"[PropertyBinding] Binding established: {_sourceProperty} → {_targetProperty}");
            }
            else
            {
                // 如果验证失败，可以在这里处理（如禁用组件）
                Debug.LogError($"[PropertyBinding] Cannot bind due to validation failure");
            }
        }

        protected override void OnShutdown()
        {
            UnBind();
        }

        private bool ValidateBindings()
        {
            if(_validationResult != null)return _validationResult.Value;

            // DI解析通道：当_source未拖入时，从DI容器解析纯C# ViewModel
            if (_source == null && !string.IsNullOrEmpty(_sourceTypeName))
            {
                _diSource = ResolveFromDI(_sourceTypeName);
            }

            if (GetActualSource() == null)
            {
                Debug.LogError("Source is null");
                _validationResult = false;
                return false;
            }

            if (_targetComponent == null)
            {
                Debug.LogError("Target Component is null");
                _validationResult = false;
                return false;
            }

            if (string.IsNullOrEmpty(_sourceProperty) || string.IsNullOrEmpty(_targetProperty))
            {
                Debug.LogError("Property is null or empty");
                _validationResult = false;
                return false;
            }
            _sourcePropertyInfo = GetActualSource().GetType().GetProperty(_sourceProperty);
            if (_sourcePropertyInfo == null)
            {
                Debug.LogError($"Source property '{_sourceProperty}' not found on {GetActualSource().GetType().Name}");
                _validationResult = false;
                return false;
            }
            _targetPropertyInfo = _targetComponent.GetType().GetProperty(_targetProperty);
            if (_targetPropertyInfo == null)
            {
                Debug.LogError($"Target property '{_targetProperty}' not found on {_targetComponent.GetType().Name}");
                _validationResult = false;
                return false;
            }
            _validationResult = true;
            return true;
        }

        public void Bind()
        {
            _notifySource=GetActualSource() as INotifyPropertyChanged;
            if (_notifySource != null)
            {
                _notifySource.PropertyChanged += OnSourcePropertyChanged;
            }
            UpdateTarget();
            if (_mode == BindingMode.TwoWay || _mode == BindingMode.OneWayToSource)
            {
                SubscribeToUIEvents();
            }
            Debug.Log($"PropertyBinding: {_sourceProperty} → {_targetProperty} ({_mode})");
        }
        public void UnBind()
        {
            if (_notifySource != null)
            {
                _notifySource.PropertyChanged -= OnSourcePropertyChanged;
                _notifySource = null;
            }
            UnsubscribeFromUIEvents();
            _sourcePropertyInfo = null;
            _targetPropertyInfo = null;
            Debug.Log($"PropertyUnBinding: {_sourceProperty} → {_targetProperty}");
        }

        public void UpdateSource()
        {
            if (_mode == BindingMode.OneWay || _mode == BindingMode.OneTime)
                return;

            if (_targetPropertyInfo == null || _sourcePropertyInfo == null)
                return;
            try
            {
                var targetValue = _targetPropertyInfo.GetValue(_targetComponent);
                var sourceValue = _converter?.ConvertBack(targetValue, _sourcePropertyInfo.PropertyType) ?? targetValue;
                _sourcePropertyInfo.SetValue(GetActualSource(), sourceValue);
                Debug.Log($"PropertyBinding: UI→ViewModel Update {_targetProperty} → {_sourceProperty} = {sourceValue}");
            }catch(Exception ex)
            {
                Debug.LogError($"PropertyBinding: Update Source Property Failed - {ex.Message}", this);
            }
        }

        public void UpdateTarget()
        {
            if (_sourcePropertyInfo == null || _targetPropertyInfo == null)
                return;

            try
            {
                // 获取ViewModel当前值
                var sourceValue = _sourcePropertyInfo.GetValue(GetActualSource());

                // 应用转换器（如果有）
                var targetValue = _converter?.Convert(sourceValue, _targetPropertyInfo.PropertyType) ?? sourceValue;

                // 设置UI属性值
                _targetPropertyInfo.SetValue(_targetComponent, targetValue);

                Debug.Log($"PropertyBinding: ViewModel→UI更新 {_sourceProperty} → {_targetProperty} = {targetValue}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"PropertyBinding: 更新目标属性失败 - {ex.Message}", this);
            }
        }

        private void SubscribeToUIEvents()
        {
            // InputField文本变更
            if (_targetComponent is InputField inputField)
            {
                inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
            }
            // Toggle状态变更
            else if (_targetComponent is Toggle toggle)
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
            // Slider值变更
            else if (_targetComponent is Slider slider)
            {
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            // Dropdown选择变更
            else if (_targetComponent is Dropdown dropdown)
            {
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
        }
        private void UnsubscribeFromUIEvents()
        {
            if (_targetComponent is InputField inputField)
            {
                inputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            }
            else if (_targetComponent is Toggle toggle)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
            else if (_targetComponent is Slider slider)
            {
                slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }
            else if (_targetComponent is Dropdown dropdown)
            {
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }
        }
        private void OnInputFieldValueChanged(string value) => UpdateSource();
        private void OnToggleValueChanged(bool value) => UpdateSource();
        private void OnSliderValueChanged(float value) => UpdateSource();
        private void OnDropdownValueChanged(int value) => UpdateSource();
        private void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == _sourceProperty)
            {
                UpdateTarget();
            }
        }

        /// <summary>
        /// 从DI容器解析纯C# ViewModel（通过注入的 IServiceProvider，不走静态定位器）
        /// </summary>
        private object ResolveFromDI(string typeName)
        {
            var scope = _scopeProvider?.CurrentScope;
            var provider = scope?.ServiceProvider ?? (IServiceProvider)_serviceProvider;

            if (provider == null)
            {
                Debug.LogWarning("[PropertyBinding] 无可用 ServiceProvider");
                return null;
            }

            var type = Type.GetType(typeName);
            if (type == null)
                type = Type.GetType($"{typeName}, Assembly-CSharp");

            if (type == null)
            {
                Debug.LogError($"[PropertyBinding] 无法找到类型: {typeName}");
                return null;
            }

            var instance = provider.GetService(type);
            if (instance == null)
            {
                Debug.LogError($"[PropertyBinding] DI容器中未注册: {typeName}");
                return null;
            }

            Debug.Log($"[PropertyBinding] DI解析成功: {typeName} (scope={scope != null})");
            return instance;
        }
    }
}