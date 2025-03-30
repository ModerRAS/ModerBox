namespace ModerBox.Comtrade.Harmonic {
    public class Util {
        public static IEnumerable<double[]> ChunkArray(double[] array, int chunkSize) {
            return array
                .Select((value, index) => new { value, index })
                .GroupBy(x => x.index / chunkSize)
                .Select(g => g.Select(x => x.value).ToArray());
        }
    }
}
