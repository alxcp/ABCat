using System.ComponentModel;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.Shared
{
    [UsedImplicitly]
    [DisplayName("Основные настройки")]
    public class MainConfig : Config
    {
        private int _groupActualityPeriod;
        private int _recordActualityPeriod;

        public override string DisplayName => "Основные настройки";

        [DisplayName("Период актуальности группы, дней")]
        [Description(
            "Период, в течении которого загруженная группа записей считается актуальной и не загружается повторно при обновлении"
        )]
        public int GroupActualityPeriod
        {
            get => _groupActualityPeriod;
            set
            {
                if (value == _groupActualityPeriod) return;
                _groupActualityPeriod = value;
                OnPropertyChanged();
            }
        }

        [DisplayName("Период актуальности записи, дней")]
        [Description(
            "Период, в течении которого загруженная запись считается актуальной и не загружается повторно при обновлении"
        )]
        public int RecordActualityPeriod
        {
            get => _recordActualityPeriod;
            set
            {
                if (value == _recordActualityPeriod) return;
                _recordActualityPeriod = value;
                OnPropertyChanged();
            }
        }

        public override bool CheckAndFix()
        {
            if (RecordActualityPeriod == 0) RecordActualityPeriod = 365;
            if (GroupActualityPeriod == 0) GroupActualityPeriod = 1;

            return true;
        }
    }
}