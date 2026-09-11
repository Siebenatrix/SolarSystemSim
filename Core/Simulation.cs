namespace SolarSystemSim.Core;

public static class Simulation
{
    public record struct StateVectors(Vector2D Position, Vector2D Velocity, double Mass);

    public static StateVectors[] GetNextStepEuler(StateVectors[] bodyStatesLastFrame, double gravConst, double dt)
    {
        int count = bodyStatesLastFrame.Length;
        StateVectors[] newStates = new StateVectors[bodyStatesLastFrame.Length];
        Vector2D[] accelerations = new Vector2D[count];
        for (var i = 0; i < count; i++)
        {
            var thisBody = bodyStatesLastFrame[i];
            Vector2D totalAcceleration = Vector2D.Zero;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                var otherBody = bodyStatesLastFrame[j];
                Vector2D distanceVector = otherBody.Position - thisBody.Position;
                double distanceSquared = distanceVector.LengthSquared();

                if (distanceSquared < 0.01f) continue;

                Vector2D dir = Vector2D.Normalize(distanceVector);
                double accelerationMagnitude = (gravConst * otherBody.Mass) / distanceSquared;
                totalAcceleration += dir * accelerationMagnitude;
            }

            accelerations[i] = totalAcceleration;
        }

        for (int i = 0; i < count; i++)
        {
            var thisBody = bodyStatesLastFrame[i];
            Vector2D newVelocity = thisBody.Velocity + accelerations[i] * dt;
            Vector2D newPosition = thisBody.Position + newVelocity * dt;

            newStates[i] = new StateVectors(newPosition, newVelocity, thisBody.Mass);
        }

        return newStates;
    }

    public static StateVectors[] GetNextStepRk4(StateVectors[] bodyStatesLastFrame, double gravConst, double dt)
    {
        int count = bodyStatesLastFrame.Length;
        var newStates = new StateVectors[count];

        var k1Vel = new Vector2D[count];
        var k2Vel = new Vector2D[count];
        var k3Vel = new Vector2D[count];
        var k4Vel = new Vector2D[count];

        var tempStates = new StateVectors[count];

        var k1Acc = ComputeAccelerations(bodyStatesLastFrame, gravConst);
        for (int i = 0; i < count; i++)
        {
            k1Vel[i] = bodyStatesLastFrame[i].Velocity;
        }

        for (int i = 0; i < count; i++)
        {
            var tempPos = bodyStatesLastFrame[i].Position + k1Vel[i] * (dt / 2.0);
            var tempVel = bodyStatesLastFrame[i].Velocity + k1Acc[i] * (dt / 2.0);
            tempStates[i] = new StateVectors(tempPos, tempVel, bodyStatesLastFrame[i].Mass);
        }

        var k2Acc = ComputeAccelerations(tempStates, gravConst);
        for (int i = 0; i < count; i++)
        {
            k2Vel[i] = tempStates[i].Velocity;
        }

        for (int i = 0; i < count; i++)
        {
            var tempPos = bodyStatesLastFrame[i].Position + k2Vel[i] * (dt / 2.0);
            var tempVel = bodyStatesLastFrame[i].Velocity + k2Acc[i] * (dt / 2.0);
            tempStates[i] = new StateVectors(tempPos, tempVel, bodyStatesLastFrame[i].Mass);
        }

        var k3Acc = ComputeAccelerations(tempStates, gravConst);
        for (int i = 0; i < count; i++)
        {
            k3Vel[i] = tempStates[i].Velocity;
        }

        for (int i = 0; i < count; i++)
        {
            var tempPos = bodyStatesLastFrame[i].Position + k3Vel[i] * dt;
            var tempVel = bodyStatesLastFrame[i].Velocity + k3Acc[i] * dt;
            tempStates[i] = new StateVectors(tempPos, tempVel, bodyStatesLastFrame[i].Mass);
        }

        var k4Acc = ComputeAccelerations(tempStates, gravConst);
        for (int i = 0; i < count; i++)
        {
            k4Vel[i] = tempStates[i].Velocity;
        }

        for (int i = 0; i < count; i++)
        {
            var initial = bodyStatesLastFrame[i];
            Vector2D finalPos = initial.Position + (k1Vel[i] + k2Vel[i] * 2.0 + k3Vel[i] * 2.0 + k4Vel[i]) * (dt / 6.0);
            Vector2D finalVel = initial.Velocity + (k1Acc[i] + k2Acc[i] * 2.0 + k3Acc[i] * 2.0 + k4Acc[i]) * (dt / 6.0);

            newStates[i] = new StateVectors(finalPos, finalVel, initial.Mass);
        }

        return newStates;
    }

    private static Vector2D[] ComputeAccelerations(StateVectors[] states, double gravConst)
    {
        int count = states.Length;
        var accelerations = new Vector2D[count];

        for (int i = 0; i < count; i++)
        {
            var thisBody = states[i];
            var totalAcceleration = Vector2D.Zero;

            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                var otherBody = states[j];
                var distanceVector = otherBody.Position - thisBody.Position;
                double distanceSquared = distanceVector.LengthSquared();

                if (distanceSquared < 0.01) continue;

                var dir = Vector2D.Normalize(distanceVector);
                double accelerationMagnitude = (gravConst * otherBody.Mass) / distanceSquared;
                totalAcceleration += dir * accelerationMagnitude;
            }

            accelerations[i] = totalAcceleration;
        }

        return accelerations;
    }
}