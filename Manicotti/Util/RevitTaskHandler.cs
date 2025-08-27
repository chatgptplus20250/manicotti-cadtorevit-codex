using Autodesk.Revit.UI;
using System;

namespace Manicotti.Util
{
    /// <summary>
    /// A generic implementation of IExternalEventHandler.
    /// Allows running any Action<UIApplication> in a valid Revit API context.
    /// </summary>
    public class RevitTaskHandler : IExternalEventHandler
    {
        private Action<UIApplication> _task;
        private readonly ExternalEvent _externalEvent;

        public RevitTaskHandler()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        /// <summary>
        /// Raises the external event and schedules the task to be run.
        /// </summary>
        /// <param name="task">The action to be executed.</param>
        public void Raise(Action<UIApplication> task)
        {
            _task = task;
            _externalEvent.Raise();
        }

        /// <summary>
        /// This method is called by Revit when the external event is raised.
        /// </summary>
        public void Execute(UIApplication app)
        {
            try
            {
                _task?.Invoke(app);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Revit Task Error", ex.Message);
            }
        }

        public string GetName()
        {
            return "Manicotti Revit Task Handler";
        }
    }
}
