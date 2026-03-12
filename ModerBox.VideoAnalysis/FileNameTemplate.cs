namespace ModerBox.VideoAnalysis {
    public static class FileNameTemplate {
        public static string Apply(string template, string videoFilePath, int index = 1) {
            var fileName = Path.GetFileNameWithoutExtension(videoFilePath);
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return template
                .Replace("{filename}", fileName)
                .Replace("{index}", index.ToString())
                .Replace("{date}", date);
        }
    }
}
