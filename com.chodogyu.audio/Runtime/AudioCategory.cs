namespace CDG.Audio
{
    /// <summary>
    /// Audio Framework에서 오디오 항목의 재생 용도를 구분하는 카테고리입니다.
    /// BGM과 SFX는 서로 다른 재생 방식과 상태 관리 규칙을 사용합니다.
    /// </summary>
    public enum AudioCategory
    {
        /// <summary>
        /// 장시간 재생되며 전환, 반복, 일시정지 등의 상태 관리가 필요한 배경 음악입니다.
        /// </summary>
        Bgm = 0,

        /// <summary>
        /// 짧은 시간 동안 재생되며 여러 소리를 동시에 재생할 수 있는 효과음입니다.
        /// </summary>
        Sfx = 1
    }
}