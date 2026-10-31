using DellarteDellaGuerra.Domain.PoliticalMap.Labels;
using TaleWorlds.Library;

namespace DellarteDellaGuerra.PoliticalMap.Labels
{
    /// <summary>One region label: its text, its anchor on screen in pixels, and its font step.</summary>
    public class PoliticalMapLabelVM : ViewModel
    {
        private float _positionX;
        private float _positionY;
        private bool _isShown;
        private int _sizeIndex = -1;

        public PoliticalMapLabelVM(string text)
        {
            Text = text;
        }

        [DataSourceProperty] public string Text { get; }

        [DataSourceProperty]
        public float PositionX
        {
            get => _positionX;
            set
            {
                if (value == _positionX) return;
                _positionX = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public float PositionY
        {
            get => _positionY;
            set
            {
                if (value == _positionY) return;
                _positionY = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty]
        public bool IsShown
        {
            get => _isShown;
            set
            {
                if (value == _isShown) return;
                _isShown = value;
                OnPropertyChangedWithValue(value);
            }
        }

        [DataSourceProperty] public bool IsSize0 => _sizeIndex == 0;
        [DataSourceProperty] public bool IsSize1 => _sizeIndex == 1;
        [DataSourceProperty] public bool IsSize2 => _sizeIndex == 2;
        [DataSourceProperty] public bool IsSize3 => _sizeIndex == 3;
        [DataSourceProperty] public bool IsSize4 => _sizeIndex == 4;
        [DataSourceProperty] public bool IsSize5 => _sizeIndex == 5;
        [DataSourceProperty] public bool IsSize6 => _sizeIndex == 6;

        /// <summary>Shows the label centred on a screen point, in one of the layout's font steps.</summary>
        public void Place(float x, float y, int sizeIndex)
        {
            // The anchor is 2x2 pixels; the text widget is centred on it.
            PositionX = x - 1f;
            PositionY = y - 1f;
            SetSize(sizeIndex);
            IsShown = true;
        }

        public void Hide() => IsShown = false;

        private void SetSize(int sizeIndex)
        {
            if (sizeIndex == _sizeIndex) return;
            int old = _sizeIndex;
            _sizeIndex = sizeIndex;
            Notify(old);
            Notify(sizeIndex);
        }

        private void Notify(int sizeIndex)
        {
            if (sizeIndex < 0 || sizeIndex >= PoliticalMapLabelLayout.FontSizes.Count) return;
            OnPropertyChangedWithValue(_sizeIndex == sizeIndex, "IsSize" + sizeIndex);
        }
    }
}
