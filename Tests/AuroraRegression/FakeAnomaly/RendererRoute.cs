namespace ClientPlugin.Backend
{
    public static class RendererRoute
    {
        public static int Active { get; set; }
        public static bool UseDx11Device => Active == 1;
        public static bool UseGuestImages => Active == 2;
    }
}
