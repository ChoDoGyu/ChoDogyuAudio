namespace CDG.Audio
{
    /// <summary>
    /// Audio Framework에서 사용하는 안정적인 오류 코드 모음입니다.
    /// 외부 코드에서는 오류 메시지보다 오류 코드를 기준으로 실패 원인을 구분할 수 있습니다.
    /// </summary>
    public static class AudioErrorCodes
    {
        /// <summary>
        /// Audio ID가 비어 있거나 유효하지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidId = "AUDIO_INVALID_ID";

        /// <summary>
        /// 재생을 요청한 AudioClip이 null이거나 유효하지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidClip = "AUDIO_INVALID_CLIP";

        /// <summary>
        /// AudioController에 Audio Catalog가 지정되지 않았는데 ID 기반 재생을 요청했을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string CatalogNotAssigned = "AUDIO_CATALOG_NOT_ASSIGNED";

        /// <summary>
        /// 요청한 Audio ID가 Audio Catalog에 등록되어 있지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string NotFound = "AUDIO_NOT_FOUND";

        /// <summary>
        /// 요청한 재생 방식과 Audio Catalog에 등록된 카테고리가 일치하지 않을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string CategoryMismatch = "AUDIO_CATEGORY_MISMATCH";

        /// <summary>
        /// Audio Catalog에 동일한 Audio ID가 두 번 이상 등록되어 있을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string DuplicateId = "AUDIO_DUPLICATE_ID";

        /// <summary>
        /// Audio Catalog에 잘못된 항목이나 설정값이 포함되어 있을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string InvalidCatalog = "AUDIO_INVALID_CATALOG";

        /// <summary>
        /// 모든 SFX Voice가 사용 중이며 현재 Overflow Policy로 새로운 재생 요청을 처리할 수 없을 때 사용하는 오류 코드입니다.
        /// </summary>
        public const string NoAvailableSfxVoice = "AUDIO_NO_AVAILABLE_SFX_VOICE";
    }
}