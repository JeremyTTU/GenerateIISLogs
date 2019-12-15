using AnyConsole;

namespace GenerateIISLogs.Utility
{
    public class ProgressComponent : IComponent
    {
        private string _value;
        private ConsoleDataContext _dataContext;
        private ProgressControl _Progress;

        public ProgressControl Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                _dataContext.SetData<ProgressControl>("ProgressControl", value);
            }
        }

        public bool HasUpdates { get; private set; }

        public bool HasCustomThreadManagement => false;

        public string Render(object parameters)
        {
            // called when HasUpdates=true
            try
            {
                return _value;
            }
            finally
            {
                HasUpdates = false;
            }
        }

        public void Setup(ConsoleDataContext dataContext, string name, IExtendedConsole console)
        {
            _dataContext = dataContext;
        }

        //public void Tick(ulong tickCount)
        //{
        //    if (Progress == null) return;
        //    var newValue = Progress.GetStatus();
        //    if (!newValue.Equals(_value))
        //    {
        //        _value = newValue;
        //        HasUpdates = true;
        //    }
        //}

        public void Tick(ulong tickCount)
        {
            // called when the console says you can update data
            if (_Progress == null)
            {
                // cache the game server context reference so we don't need to keep looking it up
                var progress = _dataContext.GetData<ProgressControl>("ProgressControl");
                if (progress != null)
                    _Progress = progress;
            }
            else
            {
                var newValue = _Progress.GetStatus();
                if (!newValue.Equals(_value))
                {
                    _value = newValue;
                    HasUpdates = true;
                }
            }
        }
    }
}
