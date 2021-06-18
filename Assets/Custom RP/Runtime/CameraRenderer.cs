using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.CustomRenderPipeline
{

    public class CameraRenderer
    {
        private ScriptableRenderContext context;
        private Camera camera;

        const string bufferName = "Render Camera";
        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        CullingResults cullingResults;

        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        static ShaderTagId[] legacyShaderTagIds = {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        public void Render(ScriptableRenderContext context, Camera camera)
        {
            this.context = context;
            this.camera = camera;

#if UNITY_EDITOR
            PrepareForSceneWindow();
#endif

            if (!Cull())
                return;

            GraphicsSettings.useScriptableRenderPipelineBatching = false;

            Setup();
            DrawVisibleGeometry();
#if UNITY_EDITOR
            DrawGizmos();
            DrawUnsupportedShaders();
#endif
            Submit();
        }

        void DrawVisibleGeometry()
        {
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );

            context.DrawSkybox(camera);

            //var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
            //var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], sortingSettings);
            //var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);

            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            //drawingSettings.SetShaderPassName(1, legacyShaderTagIds[0]);
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            context.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );
        }

        void DrawUnsupportedShaders()
        {
            var drawingSettings = new DrawingSettings(
                legacyShaderTagIds[0], new SortingSettings(camera)
            );
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);

            for (int i = 0; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
                context.DrawRenderers(
                    cullingResults, ref drawingSettings, ref filteringSettings
                );
            }
        }

#if UNITY_EDITOR
        string SampleName { get; set; }

        void PrepareBuffer()
        {
            buffer.name = SampleName = camera.name;
        }

        void PrepareForSceneWindow()
        {
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }

        void DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
#else
        const string SampleName = bufferName;
#endif

        bool Cull()
        {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                cullingResults = context.Cull(ref p);
                return true;
            }
            return false;
        }

        void Submit()
        {
            buffer.EndSample(SampleName);
            //ExecuteBuffer();
            context.Submit();
        }

        void Setup()
        {
            context.SetupCameraProperties(camera);
            buffer.ClearRenderTarget(camera.clearFlags <= CameraClearFlags.Depth, camera.clearFlags == CameraClearFlags.Color, camera.clearFlags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear);
            buffer.BeginSample(SampleName);
            ExecuteBuffer();
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}
