using System;

/// <summary>Risupallon potkulento ja laskeutumisen jälkeen jatkuva vierintä.</summary>
public struct TumbleweedFlight
{
    public float VelocityX { get; private set; }
    public float Height { get; private set; }
    public bool Airborne => remaining > 0f;
    float remaining, verticalSpeed, gravity, rollingSpeed;

    public TumbleweedFlight(float speed)
    {
        rollingSpeed = Math.Abs(speed);
        VelocityX = -rollingSpeed;
        Height = remaining = verticalSpeed = gravity = 0f;
    }

    public bool Kick(float launchSpeed, float upSpeed, float acceleration)
    {
        if (Airborne) return false;
        VelocityX = (VelocityX <= 0f ? 1f : -1f) * Math.Abs(launchSpeed);
        verticalSpeed = Math.Max(0.1f, upSpeed);
        gravity = Math.Max(0.1f, acceleration);
        remaining = 2f * verticalSpeed / gravity;
        Height = 0f;
        return true;
    }

    /// <returns>Vaakasuunnassa kuljettu matka tällä päivityksellä.</returns>
    public float Step(float dt)
    {
        if (dt <= 0f) return 0f;
        if (!Airborne) return VelocityX * dt;
        float flightTime = Math.Min(dt, remaining);
        float distance = VelocityX * flightTime;
        Height = Math.Max(0f, Height + verticalSpeed * flightTime - 0.5f * gravity * flightTime * flightTime);
        verticalSpeed -= gravity * flightTime;
        remaining -= flightTime;
        if (remaining <= 0f)
        {
            Height = 0f;
            VelocityX = (VelocityX >= 0f ? 1f : -1f) * rollingSpeed;
            distance += VelocityX * (dt - flightTime);
        }
        return distance;
    }
}
