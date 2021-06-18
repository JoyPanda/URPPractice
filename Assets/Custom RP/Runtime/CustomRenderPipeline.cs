namespace UnityEngine.Rendering.CustomRenderPipeline
{
    public sealed class CustomRenderPipeline : RenderPipeline
    {
        private CameraRenderer renderer = new CameraRenderer();

        public CustomRenderPipeline(CustomRenderPipelineAsset asset)
        {
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            //ShaderBindings.SetPerFrameShaderVariables(context);
            foreach (Camera camera in cameras)
            {
                renderer.Render(context, camera);
            }
        }
    }
}
