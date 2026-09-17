namespace CDG.Audio.Editor
{
    /// <summary>
    /// Audio Catalog Editor 검증에서 발견된 하나의 문제를 나타냅니다.
    /// 오류 코드와 Entry 위치를 함께 보관하여 Inspector와 테스트에서 동일한 검증 결과를 사용할 수 있습니다.
    /// </summary>
    internal readonly struct AudioCatalogValidationIssue
    {
        /// <summary>
        /// 문제의 종류를 식별하는 Audio Framework 오류 코드를 반환합니다.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 문제가 발견된 Audio Entry의 인덱스를 반환합니다.
        /// </summary>
        public int EntryIndex { get; }

        /// <summary>
        /// 사용자가 문제를 확인할 수 있는 설명 메시지를 반환합니다.
        /// </summary>
        public string Message { get; }

        public AudioCatalogValidationIssue(string code, int entryIndex, string message)
        {
            Code = code;
            EntryIndex = entryIndex;
            Message = message;
        }
    }
}