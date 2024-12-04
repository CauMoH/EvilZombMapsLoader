using EvilZombMapsLoader.Extensions.Attributes;

namespace EvilZombMapsLoader.Enums
{
    public enum LoadProcessStates
    {
        [Description("Неизвестно")]
        Unknown,

        [Description("Готов к загрузке")]
        ReadyToLoading,

        [Description("Загрузка...")]
        Loading,

        [Description("Загружено")]
        Loaded,

        [Description("Загружено с ошибками")]
        LoadedWithError,

        [Description("Отмена загрузки...")]
        Cancel,

        [Description("Загрузка отменена")]
        Canceled
    }
}
