using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace PipelineMod.Common.PLBlockEntityRenderer;

public class PipelineMPRenderer : MechBlockRenderer
{
    private readonly List<PipelineMPRComponent> components = [];
    
    private readonly Vec3f axisCenter = new(0.5f, 0.5f, 0.5f);


    public PipelineMPRenderer(ICoreClientAPI capi, MechanicalPowerMod mechanicalPowerMod, Block textureSourceBlock, CompositeShape shapeLoc) : base(capi, mechanicalPowerMod)
    {
        // Main axle, centered.
        components.Add(
            new PipelineMPRComponent(capi, shapeLoc,  textureSourceBlock, "pipelinemod:shapes/pump2-engine-primarygear.json", 42000)
            {
                RotXFunc = dev => dev.AngleRad * -Math.Abs(dev.MainGearAxis[0]),
                RotZFunc = dev => dev.AngleRad * -Math.Abs(dev.MainGearAxis[2]),
                AxisFunc = dev => axisCenter,
                UseInitialTransform = false
            }
        );

        // This is the 'location' of the 2nd axle from the NW corner. (in model space). Y is 0.5
        const float xN = 0.0625f;
        const float zN = 0.78125f;
        var facing = BlockFacing.FromCode(textureSourceBlock.Variant["side"]);
        
        // Shifting the location to account for block orientation.
        var secondaryGearAxis1 = facing.Code switch
        {
            "north" => new Vec3f(xN, 0.5f, zN),
            "south" => new Vec3f(1f - xN, 0.5f, 1f - zN),
            "west" =>  new Vec3f(zN, 0.5f, 1f - xN),
            "east" =>  new Vec3f(1f - zN, 0.5f, xN),
            _      =>  new Vec3f(xN, 0.5f, zN)
        };
        
        // Secondary axle, offset
        components.Add(
            new PipelineMPRComponent(capi, shapeLoc,  textureSourceBlock, "pipelinemod:shapes/pump2-engine-secondarygear.json", 42000)
            {
                RotXFunc = dev => dev.AngleSecondary * Math.Abs(dev.SecondaryGearAxis[0]),
                RotZFunc = dev => dev.AngleSecondary * Math.Abs(dev.SecondaryGearAxis[2]),
                AxisFunc = dev => secondaryGearAxis1,
                UseInitialTransform = false
            }
        );

        // The primary 'slider' aka piston rod
        const float sliderTravel = 4.5f / 16f;
        
        components.Add(
            new PipelineMPRComponent(capi, shapeLoc,  textureSourceBlock, "pipelinemod:shapes/pump2-engine-slider.json", 42000)
            {
                RotXFunc = dev => 0f,
                RotZFunc = dev => 0f,
                AxisFunc = dev => axisCenter,
                UseInitialTransform = false,
                TranslationFunc = dev =>
                {
                    var t = dev.AngleSecondary;
                    var offsetZ = (GameMath.Cos(t) * 0.5f - 0.5f) * sliderTravel;
                    return new Vec3f(offsetZ * dev.SecondaryGearAxis[2], 0f, offsetZ * dev.SecondaryGearAxis[0]);
                }
            }
        );
        
        // The connecting rod between piston rod and secondary axle gear.
        components.Add(
            new PipelineMPRComponent(capi, shapeLoc,  textureSourceBlock, "pipelinemod:shapes/pump2-engine-connectingrod.json", 42000)
            {
                RotXFunc = dev => Math.Abs(dev.SecondaryGearAxis[0]) != 0
                    ? GameMath.Sin(dev.AngleSecondary * Math.Abs(dev.SecondaryGearAxis[0])) * GameMath.PI * 0.1f
                    : 0f,
                RotZFunc = dev => Math.Abs(dev.SecondaryGearAxis[2]) != 0 
                    ? GameMath.Sin(dev.AngleSecondary * Math.Abs(dev.SecondaryGearAxis[2])) * GameMath.PI * 0.1f 
                    : 0f,
                AxisFunc = dev => axisCenter,
                UseInitialTransform = false,
                TranslationFunc = dev =>
                {
                    var t = dev.AngleSecondary;
                    var offsetZ = (GameMath.Cos(t) * 0.5f - 0.5f) * sliderTravel;
                    return new Vec3f(offsetZ * dev.SecondaryGearAxis[2], 0f, offsetZ * dev.SecondaryGearAxis[0]);
                }
            }
        );
        
    }

    // Runs the transformation code of each component. Uses the primary axle to figure out which way things are located.
    protected override void UpdateLightAndTransformMatrix(int index, Vec3f distToCamera, float rotRad, IMechanicalPowerRenderable dev)
    {
        var rot1 = dev.AngleRad;
        float axX = -Math.Abs(dev.AxisSign[0]);
        float axZ = -Math.Abs(dev.AxisSign[2]);

        TransformMatrix(distToCamera, axX == 0 ? rot1 * 2f : 0f, axZ == 0 ? -rot1 * 2f : 0f, axisCenter);

        foreach (var component in components)
        {
            var engine = (dev as IPipelineMPPumpEngineRenderable)!;
            
            var rotX = component.RotXFunc(engine);
            var rotZ = component.RotZFunc(engine);
            var axis = component.AxisFunc(engine);
            float[]? initialTransform = null;

            if (component.UseInitialTransform)
                initialTransform = (float[])tmpMat.Clone();

            var translation = component.TranslationFunc?.Invoke(engine);
            
            UpdateLightAndTransformMatrix(
                component.Buffer.Values,
                index,
                distToCamera,
                dev.LightRgba,
                rotX,
                rotZ,
                axis,
                initialTransform,
                translation
            );
        }
        
    }

    public override void OnRenderFrame(float deltaTime, IShaderProgram prog)
    {
        UpdateCustomFloatBuffer();

        if (quantityBlocks <= 0) return;

        // Calls the client tick on the devices as they want to do stuff each frame.
        foreach (var device in renderedDevices.Values)
        {
            // this is the block behavior
            (device as IPipelineMPPumpEngineRenderable)?.ClientTick(deltaTime);
        }

        // Calls the render method for each component of this block.
        foreach (var component in components)
        {
            component.Buffer.Count = quantityBlocks * 20;
            updateMesh.CustomFloats = component.Buffer;

            capi.Render.UpdateMesh(component.Mesh, updateMesh);
            capi.Render.RenderMeshInstanced(component.Mesh, quantityBlocks);
        }
    }

    // Below here are default render code from the creative rotor renderer.
    private void TransformMatrix(Vec3f distToCamera, float rotX, float rotZ, Vec3f axis)
    {
        Mat4f.Identity(tmpMat);
        Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
        quat[0] = 0.0;
        quat[1] = 0.0;
        quat[2] = 0.0;
        quat[3] = 1.0;
        
        if (rotX != 0.0)
            Quaterniond.RotateX(quat, quat, rotX);
        if (rotZ != 0.0)
            Quaterniond.RotateZ(quat, quat, rotZ);
        
        for (var index = 0; index < quat.Length; ++index)
            qf[index] = (float) quat[index];
        
        Mat4f.Mul(tmpMat, tmpMat, Mat4f.FromQuat(rotMat, qf));
        Mat4f.Translate(tmpMat, tmpMat, -axis.X, -axis.Y, -axis.Z);
    }
    
    protected void UpdateLightAndTransformMatrix(
        float[] values,
        int index,
        Vec3f distToCamera,
        Vec4f lightRgba,
        float rotX,
        float rotZ,
        Vec3f axis,
        float[]? initialTransform,
        Vec3f? translation
    )
    {
        if (initialTransform == null)
        {
            Mat4f.Identity(tmpMat);
            Mat4f.Translate(tmpMat, tmpMat, distToCamera.X + axis.X, distToCamera.Y + axis.Y, distToCamera.Z + axis.Z);
        }
        else
            Mat4f.Translate(tmpMat, tmpMat, axis.X, axis.Y, axis.Z);

        if (translation != null)
        {
            Mat4f.Translate(tmpMat, tmpMat, translation.X, translation.Y, translation.Z);
        }
        
        quat[0] = 0.0;
        quat[1] = 0.0;
        quat[2] = 0.0;
        quat[3] = 1.0;
        if (rotX != 0.0)
            Quaterniond.RotateX(quat, quat, rotX);
        if (rotZ != 0.0)
            Quaterniond.RotateZ(quat, quat, rotZ);
        for (var index1 = 0; index1 < quat.Length; ++index1)
            qf[index1] = (float) quat[index1];
        Mat4f.Mul(tmpMat, tmpMat, Mat4f.FromQuat(rotMat, qf));
        Mat4f.Translate(tmpMat, tmpMat, -axis.X, -axis.Y, -axis.Z);
        var index2 = index * 20;
        values[index2] = lightRgba.R;
        int num1;
        values[num1 = index2 + 1] = lightRgba.G;
        int num2;
        values[num2 = num1 + 1] = lightRgba.B;
        int num3;
        values[num3 = num2 + 1] = lightRgba.A;
        for (var index3 = 0; index3 < 16 /*0x10*/; ++index3)
            values[++num3] = tmpMat[index3];
    }

    public override void Dispose()
    {
        base.Dispose();
        
        // Also dispose each component.
        foreach (var component in components)
        {
            component.Mesh.Dispose();
        }
    }
}