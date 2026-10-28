using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// QUÉ HACE
///   Base común de las pruebas PlayMode del nivel: carga Nivel01_Santuario, crea un teclado simulado
///   y ofrece ayudas (teletransportar a Kael, buscar un fragmento, "recogerlo").
///
/// POR QUÉ EXISTE
///   Evita repetir la preparación en cada prueba. Hereda de InputTestFixture, que aísla el Input System
///   para que el teclado simulado no se mezcle con dispositivos reales.
///
/// CÓMO SE USA
///   Crear una clase de pruebas que herede de LevelTestBase y empezar cada prueba con
///   "yield return LoadLevel();". Requiere haber ejecutado Fragmento > Construir Escena.
/// </summary>
public abstract class LevelTestBase : InputTestFixture
{
    /// <summary>Escena que carga LoadLevel. Las pruebas de otro nivel sobrescriben esta propiedad.</summary>
    protected virtual string LevelScene => "Nivel01_Santuario";

    protected Keyboard keyboard;
    protected PlayerController player;
    protected CharacterController controller;

    private int previousTargetFrameRate;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();

        // En modo batch Unity corre a miles de FPS; se fija 60 para que las pruebas reflejen el juego real.
        previousTargetFrameRate = Application.targetFrameRate;
        Application.targetFrameRate = 60;
    }

    public override void TearDown()
    {
        Application.targetFrameRate = previousTargetFrameRate;
        base.TearDown();
    }

    protected IEnumerator LoadLevel(bool enemiesEnabled = false, bool livesEnabled = false)
    {
        PlayerProgress.Reset(); // el progreso es estático: cada prueba empieza como una partida nueva
        yield return SceneManager.LoadSceneAsync(LevelScene);
        yield return null;

        // Los peligros matan a Kael y arruinarían las pruebas de recorrido: se apagan salvo que se pidan.
        if (!enemiesEnabled)
        {
            foreach (Hazard hazard in Object.FindObjectsByType<Hazard>(FindObjectsSortMode.None))
            {
                hazard.enabled = false;
            }
        }

        // Las vidas recargarían la escena a mitad de prueba (algunas lanzan PlayerDied muchas veces).
        if (!livesEnabled)
        {
            foreach (LivesManager lives in Object.FindObjectsByType<LivesManager>(FindObjectsSortMode.None))
            {
                lives.enabled = false;
            }
        }

        player = Object.FindFirstObjectByType<PlayerController>();
        Assert.IsNotNull(player, "No hay un PlayerController en la escena. ¿Ejecutaste Fragmento > Construir Escena?");
        controller = player.GetComponent<CharacterController>();
    }

    /// <summary>
    /// Espera al menos "minSeconds" y luego hasta que Kael recupere el control (las cámaras de evento se lo quitan
    /// unos segundos). Llamar después de recoger un fragmento y antes de pulsar teclas.
    /// </summary>
    protected IEnumerator WaitForControl(float minSeconds)
    {
        yield return new WaitForSeconds(minSeconds);

        float timeout = 15f;
        while (player.IsInputLocked && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.Greater(timeout, 0f, "Kael no recuperó el control tras el evento.");
        yield return new WaitForSeconds(0.1f);
    }

    protected void Teleport(Vector3 position)
    {
        player.TeleportTo(position);
    }

    /// <summary>Busca el fragmento con ese id (null si ya fue recogido y destruido).</summary>
    protected Fragment FindFragment(int id)
    {
        foreach (Fragment fragment in Object.FindObjectsByType<Fragment>(FindObjectsSortMode.None))
        {
            if (fragment.Data != null && fragment.Data.Id == id)
            {
                return fragment;
            }
        }

        return null;
    }

    /// <summary>Busca un puente por el nombre de su objeto ("Puente de luz 1", "Puente de luz 5").</summary>
    protected static BridgeAppear FindBridge(string objectName)
    {
        foreach (BridgeAppear bridge in Object.FindObjectsByType<BridgeAppear>(FindObjectsSortMode.None))
        {
            if (bridge.name == objectName)
            {
                return bridge;
            }
        }

        return null;
    }

    /// <summary>Coloca a Kael sobre el fragmento (pies 1.3 m bajo su centro) para que lo absorba.</summary>
    protected void CollectFragment(int id)
    {
        Fragment fragment = FindFragment(id);
        Assert.IsNotNull(fragment, "No se encontró el fragmento " + id + " en la escena.");
        Teleport(fragment.transform.position + Vector3.down * 1.3f);
    }
}
