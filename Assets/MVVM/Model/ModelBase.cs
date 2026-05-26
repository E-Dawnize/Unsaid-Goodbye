using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Gameplay.SceneFlow;

namespace MVVM.Model
{
    public abstract class ModelBase:INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnChanged([CallerMemberName] string prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop ?? string.Empty));
    }
}