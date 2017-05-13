namespace Lightwork.Core
{
    public delegate void WorkflowCompleteHandler(object sender, WorkflowCompleteEventArgs e);

    public delegate void WorkflowExceptionHandler(object sender, WorkflowExceptionEventArgs e);
}
