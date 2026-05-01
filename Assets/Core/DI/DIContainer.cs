using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Core.Architecture;
using Core.Architecture.Interfaces;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.DI
{
    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton
    }

    public interface IScope : IDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
    public class InjectAttribute: Attribute{}

    public class ServiceDescriptor
    {
        public Type ServiceType;
        public int Id;
        public ServiceLifetime Lifetime;
        public int Order;
        public Type ImplementationType;
        [CanBeNull] public object ImplementationInstance;
        [CanBeNull] public Func<IServiceProvider, object> ImplementationFactory;

        private ServiceDescriptor(int id, Type serviceType,ServiceLifetime lifetime, int order = 0)
        {
            Id = id;
            ServiceType = serviceType;
            Order = order;
            Lifetime = lifetime;
        }
        public ServiceDescriptor(int id,Type serviceType, Type implementationType,ServiceLifetime lifetime,int order=0)
            :this(id,serviceType,lifetime,order)
        {
            ImplementationType = implementationType;
        }
        public ServiceDescriptor(int id,Type serviceType, object implementationInstance,int order=0)
            :this(id,serviceType,ServiceLifetime.Singleton,order)
        {
            ImplementationInstance = implementationInstance;
        }
        public ServiceDescriptor(int id,Type serviceType,Func<IServiceProvider, object> implementationFactory,ServiceLifetime lifetime,int order=0)
            :this(id,serviceType,lifetime,order)
        {
            ImplementationFactory = implementationFactory;
        }
    }
    public partial class DIContainer:IServiceProvider,IDisposable
    {
        private readonly ConcurrentDictionary<Type, List<ServiceDescriptor>> _serviceDescriptors = new();//服务描述符
        private readonly ConcurrentDictionary<int, object> _singletonInstances = new();//实现实例
        private readonly ConcurrentBag<IDisposable> _disposables = new();//待释放服务
        private readonly ThreadLocal<Stack<Type>> _resolveStack = new(() => new Stack<Type>());//递归依赖检测栈
        private bool _disposed;
        private static int _globalServiceId;

        private int NextId() => Interlocked.Increment(ref _globalServiceId);
        private DIContainer _parentContainer;
        public DIContainer CreateChildContainer()
         {
             var childContainer = new DIContainer();
             childContainer._parentContainer = this;
             return childContainer;
         }
        public object GetRequiredService<T>(IScope scope = null) => GetRequiredService(typeof(T),scope);

        private object GetRequiredService(Type t, IScope scope = null)
        {
            var s = scope as Scope;
            var obj = ResolveService(t, s);
            if (obj == null) throw new InvalidOperationException($"Service not registered: {t}");
            return obj;
        }
        #region 注册方法

        private void Register(ServiceDescriptor descriptor)
        {
            var list=_serviceDescriptors.GetOrAdd(descriptor.ServiceType,_=>new List<ServiceDescriptor>());
            list.Add(descriptor);
            Debug.Log(descriptor.ServiceType.FullName+" is registered");
        }
        
        public void RegisterTransient<TService, TImplementation>() where TImplementation : TService
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), typeof(TImplementation), ServiceLifetime.Transient));
        public void RegisterSingleton<TService, TImplementation>() where TImplementation : TService 
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton));
        public void RegisterScoped<TService, TImplementation>() where TImplementation : TService
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped));
        

        public void RegisterSingleton<TService>(TService implementationInstance) where TService:class
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), implementationInstance));
        
        public void RegisterTransient<TService>(Func<IServiceProvider, object> implementationFactory) where TService:class
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), sp=>implementationFactory(sp)!, ServiceLifetime.Transient));
        public void RegisterSingleton<TService>(Func<IServiceProvider, object> implementationFactory) where TService:class
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), sp=>implementationFactory(sp)!, ServiceLifetime.Singleton));
        public void RegisterScoped<TService>(Func<IServiceProvider, object> implementationFactory) where TService:class
            =>Register(new ServiceDescriptor(NextId(),typeof(TService), sp=>implementationFactory(sp)!, ServiceLifetime.Scoped));
        #endregion

        #region 服务解析接口

        [CanBeNull]
        public T? GetService<T>(IScope scope=null) => (T?)ResolveService(typeof(T),scope as Scope);
        public object GetService(Type serviceType)=>ResolveService(serviceType,null);
        public IEnumerable<T> ResolveAll<T>(IScope scope = null)
        {
            var s = scope as Scope;
            return ResolveAll(typeof(T), s).Cast<T>();
        }
        #endregion

        #region 解析

        private IEnumerable<object> ResolveAll(Type type, Scope scope)
        {
            var results = new List<object>();
            if (_serviceDescriptors.TryGetValue(type, out var descriptors))
            {
                var ordered = descriptors.ToArray();
                foreach (var descriptor in descriptors)
                {
                    var obj=ResolveService(descriptor.ServiceType, scope);
                    if (obj != null)
                    {
                        results.Add(obj);
                    }
                }
            }
            if (_parentContainer != null)
                results.AddRange(_parentContainer.ResolveAll(type, scope));
            return results;
        }
        [CanBeNull]
        private object ResolveService(Type serviceType,[CanBeNull] Scope scope)
        {
            if (_resolveStack.Value!.Contains(serviceType))
                throw new InvalidOperationException($"Circular dependency: {string.Join(" -> ", _resolveStack.Value.Reverse())} -> {serviceType}");
            
            _resolveStack.Value.Push(serviceType);
            try
            {
                return ResolveCore(serviceType, scope);
            }
            finally
            {
                _resolveStack.Value.Pop();
            }
        }

        [CanBeNull]
        private object ResolveCore(Type serviceType, [CanBeNull] Scope scope)
        {
            if (_serviceDescriptors.TryGetValue(serviceType, out var descriptors))
            {
                return ResolveDescriptor(descriptors, scope);
            }

            if (_parentContainer != null)
            {
                return _parentContainer.ResolveService(serviceType, scope);
            }
            // if (!serviceType.IsInterface && !serviceType.IsAbstract && !serviceType.IsValueType)
            // {
            //     var autoDesc = new ServiceDescriptor(serviceType, serviceType, ServiceLifetime.Transient);
            //     return ResolveDescriptor(autoDesc, scope);
            // }
            return null;
        }

        [CanBeNull]
        private object ResolveDescriptor(List<ServiceDescriptor> descriptors, [CanBeNull] Scope scope)
        {
            var descriptor = SelectDescriptor(descriptors);
            return descriptor.Lifetime switch
            {
                ServiceLifetime.Singleton => ResolveSingleton(descriptor),
                ServiceLifetime.Scoped => ResolveScope(descriptor, scope),
                ServiceLifetime.Transient => ResolveTransient(descriptor, scope)
            };
        }

        private ServiceDescriptor SelectDescriptor(List<ServiceDescriptor> descriptors)
        {
            return descriptors.First();
        }
        private object ResolveSingleton(ServiceDescriptor descriptor)
        {
            if (_singletonInstances.TryGetValue(descriptor.Id, out var instance))
            {
                return instance;
            }

            lock (_singletonInstances)
            {
                if (_singletonInstances.TryGetValue(descriptor.Id, out instance))
                {
                    return instance;
                }
                instance = CreateInstance(descriptor, null);
                _singletonInstances[descriptor.Id] = instance;
                if(instance is  IDisposable disposable)_disposables.Add(disposable);
                return instance;
            }
            
        }
        private object ResolveScope(ServiceDescriptor descriptor,Scope scope)
        {
            if (scope == null) throw new InvalidOperationException("Scoped service requires a scope.");
            Scope currentScope = scope;
            object instance;
            while(currentScope!=null){    
                if (scope.ScopedInstances.TryGetValue(descriptor.Id, out instance))
                {
                    return instance;
                }
                currentScope = currentScope._parent;
            }
            instance = CreateInstance(descriptor, scope);
            scope.ScopedInstances[descriptor.Id] = instance;
            if(instance is IDisposable disposable) scope.Disposables.Add(disposable);
            return instance;
        }

        private object ResolveTransient(ServiceDescriptor descriptor,[CanBeNull] Scope scope)
        {
            var instance = CreateInstance(descriptor, scope);
            if (instance is IDisposable disposable)
            {
                if(scope!=null)scope.Disposables.Add(disposable);
                else _disposables.Add(disposable);
            }

            return instance;
        }
        private readonly ConcurrentDictionary<Type,ConstructorInfo> _constructorsCache = new ConcurrentDictionary<Type, ConstructorInfo>();
        private object CreateInstance(ServiceDescriptor descriptor, [CanBeNull] Scope scope)
        {
            if (descriptor.ImplementationFactory != null)
            {
                return descriptor.ImplementationFactory(scope!=null?scope.ServiceProvider:this);
            }

            if (descriptor.ImplementationInstance != null)
            {
                return descriptor.ImplementationInstance;
            }
            
            var implementationType = descriptor.ImplementationType;
            var ctor=_constructorsCache.GetOrAdd(implementationType,type=>
            {
                var constructors=type.GetConstructors();
                return constructors.FirstOrDefault(c
                           => c.GetCustomAttributes(typeof(InjectAttribute), false).Length != 0) ??
                       constructors.OrderByDescending(c => c.GetParameters().Length).First();
            });
            var parameters = ctor.GetParameters();
            var paramValues = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var val=TryResolveEnumerable(paramType, scope, out var enumerableValue)?enumerableValue:ResolveService(paramType, scope);
                val=val!=null?
                    val:
                    parameters[i].HasDefaultValue?
                        parameters[i].DefaultValue:null;
                if(val==null) throw new InvalidOperationException($"Missing dependency: {paramType}");
                paramValues[i] = val;
            }
            
            var instance=ctor.Invoke(paramValues);
            Inject(instance, scope);
            // 改为注册到LifecycleRegistry
            if (instance is IInitializable or IStartable)
            {
                LifecycleRegistry.Register(instance);
            }
            return instance;
        }

        private bool TryResolveEnumerable(Type paramType, Scope scope, out object value)
        {
            value = null;

            // IEnumerable<T>
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var elemType = paramType.GetGenericArguments()[0];
                value = BuildList(elemType, ResolveAll(elemType, scope));
                return true;
            }

            // List<T>
            if (paramType.IsGenericType &&
                paramType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elemType = paramType.GetGenericArguments()[0];
                value = BuildList(elemType, ResolveAll(elemType, scope));
                return true;
            }

            // T[]
            if (paramType.IsArray)
            {
                var elemType = paramType.GetElementType();
                var items = ResolveAll(elemType, scope);
                value = BuildArray(elemType, items);
                return true;
            }

            return false;
        }
        private object BuildList(Type elemType, IEnumerable<object> items)
        {
            var listType = typeof(List<>).MakeGenericType(elemType);
            var list = (System.Collections.IList)Activator.CreateInstance(listType);
            foreach (var it in items) list.Add(it);
            return list;
        }

        private object BuildArray(Type elemType, IEnumerable<object> items)
        {
            var array = items.ToArray();
            var result = Array.CreateInstance(elemType, array.Length);
            for (int i = 0; i < array.Length; i++) result.SetValue(array[i], i);
            return result;
        }
        #endregion

    #region 资源释放
        public void Dispose()
        {
            if(_disposed)return;
            _disposed = true;
            foreach (var obj in _disposables)
            {
                obj.Dispose();
            }
            _disposables.Clear();
            _singletonInstances.Clear();
            _constructorsCache.Clear();
            _serviceDescriptors.Clear();
        }
        #endregion

        #region 依赖图验证

        /// <summary>
        /// 遍历所有已注册的 Singleton 服务，尝试解析并报告所有错误
        /// 在 Boot 阶段调用，确保 DI 配置正确后再进入游戏
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            foreach (var kv in _serviceDescriptors)
            {
                var serviceType = kv.Key;

                // 跳过生命周期基础设施
                if (serviceType == typeof(IInitializable) ||
                    serviceType == typeof(IStartable) ||
                    serviceType == typeof(ITickable))
                    continue;

                // 开放泛型无法直接解析，跳过
                if (serviceType.IsGenericTypeDefinition)
                    continue;

                foreach (var desc in kv.Value)
                {
                    // 实例注册 / 工厂注册 由开发者保证正确性
                    if (desc.ImplementationInstance != null || desc.ImplementationFactory != null)
                        continue;

                    // 抽象类型无实现可解析
                    if (desc.ImplementationType == null)
                        continue;

                    // 只验证 Singleton（Transient/Scoped 依赖运行时 Scope，不适合 Boot 验证）
                    if (desc.Lifetime != ServiceLifetime.Singleton)
                        continue;

                    try
                    {
                        ResolveService(serviceType, null);
                        result.CheckedCount++;
                        Debug.Log($"[DI Validate] OK: {serviceType.Name}");
                    }
                    catch (Exception ex)
                    {
                        var msg = $"{serviceType.Name} → {desc.ImplementationType.Name}: {Unwrap(ex).Message}";
                        result.Errors.Add(msg);
                        Debug.LogError($"[DI Validate] FAIL: {msg}");
                    }
                }
            }

            if (result.IsValid)
                Debug.Log($"[DI Validate] All {result.CheckedCount} services passed");
            else
                Debug.LogError($"[DI Validate] {result.Errors.Count}/{result.CheckedCount + result.Errors.Count} failed");

            return result;
        }

        /// <summary>
        /// 展开反射调用包装的异常，拿到真正的根因
        /// </summary>
        private static Exception Unwrap(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null)
                ex = ex.InnerException;
            return ex;
        }

        public class ValidationResult
        {
            public readonly List<string> Errors = new();
            public int CheckedCount;
            public bool IsValid => Errors.Count == 0;
        }

        #endregion

        #region 作用域实现

        public IScope CreateScope(IScope parentScope = null)=>new Scope(this,parentScope as Scope);
        internal class Scope:IScope
        {
            private readonly DIContainer _container;
            public readonly Scope _parent;
            public ConcurrentDictionary<int, object> ScopedInstances { get; } = new();
            public ConcurrentBag<IDisposable> Disposables { get; } = new();
            public IServiceProvider ServiceProvider { get; }
            private bool _disposed;
            public Scope(DIContainer container,Scope parent=null)
            {
                _container = container;
                _parent=parent;
                ServiceProvider=new ScopedServiceProvider(this);
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                foreach (var disposable in Disposables)
                {
                    disposable.Dispose();
                }
                Disposables.Clear();
                ScopedInstances.Clear();
            }
            private class ScopedServiceProvider : IServiceProvider
            {
                private readonly Scope _scope;
                public ScopedServiceProvider(Scope scope) => _scope = scope;

                public object? GetService(Type serviceType) => _scope._container.ResolveService(serviceType, _scope);
            }
        }

        #endregion
    }
    public static class ServiceProviderExtensions
    {
        public static T? GetService<T>(this IServiceProvider sp)
            => (T?)sp.GetService(typeof(T));
    }
}