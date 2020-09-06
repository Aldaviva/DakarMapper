using System.Windows;
using DakarMapper;
using DakarMapper.Data;

namespace DakarMapperUI {

    public static class PointExtensions {

        public static Point toWpfPoint(this PointDouble pointDouble) {
            return new Point(pointDouble.x, pointDouble.y);
        }

    }

}