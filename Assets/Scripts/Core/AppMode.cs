namespace MuseoAutista
{
    /// <summary>
    /// Modo de la aplicación. La versión "Joystick" (pantalla completa, sin Cardboard) se
    /// compila con el símbolo MUSEO_JOYSTICK desde "Museo Autista > Compilar APK Joystick".
    /// </summary>
    public static class AppMode
    {
        /// <summary>Solo para pruebas en el editor: fuerza el modo joystick.</summary>
        public static bool ForceJoystick;

#if MUSEO_JOYSTICK
        public static bool Joystick => true;
#else
        public static bool Joystick => ForceJoystick;
#endif
    }
}
