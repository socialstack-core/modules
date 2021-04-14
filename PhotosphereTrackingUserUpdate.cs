namespace Api.PhotosphereTracking
{
    /// <summary>
    /// Positional information of a user
    /// </summary>
    public class PhotosphereTrackingUserUpdate
    {
        public int UserId;
        public double PosX;
        public double PosY;
        public double PosZ;
        public double RotationX;
        public double RotationY;
    }
}