using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TestPreserveYRotation
{
    public struct Expectation
    {
        public Expectation(Quaternion i, Quaternion o)
        {
            this.i = i;
            this.o = o;
        }
        public Quaternion i;
        public Quaternion o;
    }

    private static Expectation[] quats = new Expectation[]
    {
        // new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(0, Vector3.right) * Quaternion.AngleAxis(10, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        // new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(0, Vector3.right) * Quaternion.AngleAxis(100, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        // new(Quaternion.Euler(0, 10, 0) * Quaternion.AngleAxis(0, Vector3.right) * Quaternion.AngleAxis(10, Vector3.forward), Quaternion.Euler(0, 10, 0)),
        // new(Quaternion.Euler(0, 100, 0) * Quaternion.AngleAxis(0, Vector3.right) * Quaternion.AngleAxis(10, Vector3.forward), Quaternion.Euler(0, 100, 0)),
        // new(Quaternion.Euler(0, 270, 0) * Quaternion.AngleAxis(0, Vector3.right) * Quaternion.AngleAxis(10, Vector3.forward), Quaternion.Euler(0, 270, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(10, Vector3.right) * Quaternion.AngleAxis(10, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(10, Vector3.right) * Quaternion.AngleAxis(5, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(50, Vector3.right) * Quaternion.AngleAxis(50, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(40, Vector3.right) * Quaternion.AngleAxis(40, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(30, Vector3.right) * Quaternion.AngleAxis(30, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(20, Vector3.right) * Quaternion.AngleAxis(20, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(15, Vector3.right) * Quaternion.AngleAxis(15, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        new(Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(50, Vector3.right) * Quaternion.AngleAxis(0, Vector3.forward), Quaternion.Euler(0, 0, 0)),
        // new(Quaternion.Euler(0, 270, 0) * Quaternion.AngleAxis(20, Vector3.right) * Quaternion.AngleAxis(50, Vector3.forward), Quaternion.Euler(0, 270, 0)),
        // new(Quaternion.Euler(0, 20, 0), Quaternion.Euler(0, 20, 0)),
        // new(Quaternion.Euler(0, 20, 0) * Quaternion.AngleAxis(90, Vector3.right), Quaternion.Euler(0, 20, 0)),
        // new(Quaternion.Euler(0, 20, 0) * Quaternion.AngleAxis(91, Vector3.right), Quaternion.Euler(0, 20, 0)),
        // new(Quaternion.Euler(0, 20, 0) * Quaternion.AngleAxis(90, Vector3.right) * Quaternion.AngleAxis(90, Vector3.forward), Quaternion.Euler(0, 20, 0)),
        // new(Quaternion.Euler(0, 20, 0) * Quaternion.AngleAxis(91, Vector3.right) * Quaternion.AngleAxis(90, Vector3.forward), Quaternion.Euler(0, 20, 0)),
        // new(Quaternion.Euler(0, 20, 0) * Quaternion.AngleAxis(91, Vector3.right) * Quaternion.AngleAxis(91, Vector3.forward), Quaternion.Euler(0, 20, 0)),
    };

    [Test]
    public void TestPreserveYRotationSimplePasses([ValueSource(nameof(quats))] Expectation expect)
    {
        var ou = GetTargetRotation(expect.i);
        
        // Assert.AreEqual(expect.o, ou);
        Assert.Less(Quaternion.Angle(expect.o, ou), 1);
    }

    private static Quaternion GetTargetRotation(Quaternion source)
    {
        //Vector3.ProjectOnPlane()
        // Does not seem to do anything
        // Quaternion.AngleAxis(-90, Vector3.right) * source
        var ein = source.eulerAngles;
        return Quaternion.Euler(0, ein.y, 0);
    }

    // // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // // `yield return null;` to skip a frame.
    // [UnityTest]
    // public IEnumerator TestPreserveYRotationWithEnumeratorPasses()
    // {
    //     // Use the Assert class to test conditions.
    //     // Use yield to skip a frame.
    //     yield return null;
    // }

    [Test]
    public async Task TestRigidbodyMove([ValueSource(nameof(quats))] Expectation expect)
    {
        var obj = new GameObject("RigidBodyHolder");
        obj.SetActive(false);
        var rigid = obj.AddComponent<Rigidbody>();
        rigid.useGravity = false;
        rigid.transform.rotation = expect.i;
        obj.SetActive(true);

        float lastAngle = 360;

        for(int i = 0; i < 1000; i++)
        {
            var curRot = rigid.transform.rotation;
            var angle = Quaternion.Angle(expect.o, curRot);
            if(lastAngle <= angle)
            {
                Assert.Fail("Angle increased / stalled " + lastAngle + ">" + angle);
                return;
            }
            if(angle < 1)
            {
                return;
            }
            lastAngle = angle;
            // var proj = curRot * Vector3.up;
            // var torque = proj;
            // torque.y = 0;
            // torque *= -(curRot.y + 1) / 2 * 100;
            // Assert.AreNotSame(proj, torque);
            var proj = curRot * Vector3.up;


            // var projX = curRot * Vector3.forward;
            // var projZ = curRot * Vector3.right;
            // var torque = new Vector3(projX.y, 0, -projZ.y) * 100;
            var norm = Mathf.Sqrt(proj.z * proj.z + proj.x * proj.x);
            var torque = new Vector3(-proj.z / norm, 0, proj.x / norm);
            rigid.AddTorque(torque);
            await Awaitable.FixedUpdateAsync();
        }
    }

    [Test]
    public void TestPredictionFromToRotation([ValueSource(nameof(quats))] Expectation expect)
    {
        var ratio = Quaternion.FromToRotation(expect.i * Vector3.up, Vector3.up);
        ratio.ToAngleAxis(out float angle, out Vector3 axis);
        var ratioExpected = expect.o * Quaternion.Inverse(expect.i);
        ratioExpected.ToAngleAxis(out float expectedAngle, out Vector3 expectedAxis);
        Assert.Less(Quaternion.Angle(expect.i * Quaternion.FromToRotation(expect.i * Vector3.up, Vector3.up) , expect.o), 1, $"Angle: {angle} Axis: {axis} | Expected Angle: {expectedAngle} Axis: {expectedAxis}");
    }

    private Quaternion RemoveRollPitch(Quaternion quat)
    {
        return quat * Quaternion.FromToRotation(quat * Vector3.up, Vector3.up);
    }

    [Test]
    public void TestRemoveRollPitch([ValueSource(nameof(quats))] Expectation expect)
    {
        var l = RemoveRollPitch(expect.i);
        var r = RemoveRollPitch(expect.o);
        Assert.AreEqual(l, r, $"{l.eulerAngles} / {r.eulerAngles}");
    }

}