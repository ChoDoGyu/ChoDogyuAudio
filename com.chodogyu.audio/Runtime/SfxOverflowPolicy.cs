namespace CDG.Audio
{
    /// <summary>
    /// 모든 SFX Voice가 사용 중일 때 새로운 SFX 재생 요청을 처리하는 방식을 정의합니다.
    /// </summary>
    public enum SfxOverflowPolicy
    {
        /// <summary>
        /// 사용 가능한 Voice가 없으면 새로운 SFX 재생 요청을 거부합니다.
        /// </summary>
        RejectNew = 0,

        /// <summary>
        /// 가장 오래전에 재생을 시작한 Voice를 중지하고 새로운 SFX를 재생합니다.
        /// </summary>
        StopOldest = 1
    }
}