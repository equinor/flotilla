    public class TransformationMatrices
    {
        // In order to get a pixel coordinate P={p1, p2} from an Echo coordinate E={e1, e2}, we need to
        // perform the transformation:
        // P = CE + D
        private double[] C = {0, 0};
        private double[] D = {0, 0};
        public TransformationMatrices(double[] p1, double[] p2, int imageHeight, int imageWidth)
        {
            double c1 = (imageWidth)/(p2[0]-p1[0]);
            double c2 = (imageHeight)/(p2[1]-p1[1]);
            double d1 = -(c1*p1[0]);
            double d2 = -(c2*p1[1]);
            C = new double[] {c1, c2};
            D = new double[] {d1, d2};
        }
    }
    