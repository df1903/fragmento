using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba automáticamente el checklist del hito H1 (prototipo) con la escena real y un teclado simulado:
///   Kael cae al suelo, camina, salta, tiene coyote time, la cámara queda detrás, el fragmento 1
///   dispara el puente una sola vez y el puente se puede cruzar.
///
/// POR QUÉ EXISTE
///   Nadie puede "ver" el juego en una máquina sin pantalla; estas pruebas son la evidencia de que el
///   prototipo funciona y avisan si un cambio posterior lo rompe.
///
/// CÓMO SE USA
///   Requiere haber ejecutado Fragmento > Construir Escena.
///   Window > General > Test Runner > PlayMode > Run All.
///   Por consola: Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults resultados.xml
/// </summary>
public class PrototipoH1Tests : LevelTestBase
{
    [UnityTest]
    public IEnumerator Kael_cae_al_suelo_y_queda_apoyado()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(1f);

        Assert.IsTrue(player.IsGrounded, "Kael debería estar apoyado en la Isla A.");
        Assert.AreEqual(0f, player.transform.position.y, 0.15f, "Kael debería estar sobre la superficie (y = 0).");
    }

    [UnityTest]
    public IEnumerator Kael_camina_hacia_adelante_con_W()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(1f);
        float startZ = player.transform.position.z;

        Press(keyboard.wKey);
        yield return new WaitForSeconds(1f);
        Release(keyboard.wKey);

        float advanced = player.transform.position.z - startZ;
        Assert.Greater(advanced, 4f, "En 1 s a 6 m/s Kael debería avanzar más de 4 m hacia el norte (+Z).");
    }

    [UnityTest]
    public IEnumerator Kael_salta_con_espacio_a_la_altura_esperada()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(1f);
        float groundY = player.transform.position.y;
        float maxY = groundY;

        Press(keyboard.spaceKey);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            maxY = Mathf.Max(maxY, player.transform.position.y);
            if (elapsed > 0.1f)
            {
                Release(keyboard.spaceKey);
            }
            yield return null;
        }

        float height = maxY - groundY;
        Assert.That(height, Is.InRange(1.4f, 2.1f), "El salto debería rondar los 1.8 m (Jump Height).");
    }

    [UnityTest]
    public IEnumerator Coyote_time_permite_saltar_justo_despues_de_dejar_el_borde()
    {
        yield return LoadLevel();
        Teleport(new Vector3(-13f, 0.05f, 0f));
        yield return new WaitForSeconds(0.5f);

        // La cámara mira al norte: la tecla A mueve a Kael hacia -X, hacia el borde oeste de la Isla A (x = -15).
        Press(keyboard.aKey);
        float timeout = 3f;
        while (player.IsGrounded && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        Assert.Greater(timeout, 0f, "Kael no llegó a salir del borde de la isla.");

        // Acaba de dejar el suelo: dentro de la ventana de coyote time el salto debe funcionar.
        float yAtEdge = player.transform.position.y;
        Press(keyboard.spaceKey);
        yield return new WaitForSeconds(0.1f);
        Release(keyboard.spaceKey);
        Release(keyboard.aKey);

        Assert.Greater(player.transform.position.y, yAtEdge,
            "Con coyote time Kael debería subir al saltar justo después de salir del borde (sin él, solo caería).");
    }

    [UnityTest]
    public IEnumerator La_camara_queda_detras_de_Kael_y_sobre_el_suelo()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(1.5f);

        Transform cameraTransform = Camera.main.transform;
        Vector3 kaelPosition = player.transform.position;

        Assert.Greater(cameraTransform.position.y, 0.5f, "La cámara no debe quedar a ras del suelo ni por debajo.");
        Assert.Less(cameraTransform.position.z, kaelPosition.z - 2f, "La cámara debe estar detrás (al sur) de Kael.");
        float distance = Vector3.Distance(cameraTransform.position, kaelPosition + Vector3.up);
        Assert.That(distance, Is.InRange(3f, 7f), "La cámara debe estar a unos 5 m de Kael.");
    }

    // ------------------------------------------------------------------ Fragmento 1 y puente

    [UnityTest]
    public IEnumerator Recoger_el_fragmento_1_hace_aparecer_el_puente_una_sola_vez()
    {
        yield return LoadLevel();
        yield return new WaitForSeconds(0.5f);

        FragmentManager manager = Object.FindFirstObjectByType<FragmentManager>();
        Fragment fragment = FindFragment(1);
        BridgeAppear bridge = Object.FindFirstObjectByType<BridgeAppear>();
        Assert.IsNotNull(manager);
        Assert.IsNotNull(fragment);
        Assert.IsNotNull(bridge);

        Transform bridgePivot = bridge.transform.Find("Pivote");
        Assert.IsFalse(bridgePivot.gameObject.activeSelf, "Antes de recoger el fragmento el puente no debe verse.");

        int requests = 0;
        System.Action<string> counter = _ => requests++;
        GameEvents.WorldEventRequested += counter;
        FragmentData data = fragment.Data;

        CollectFragment(1);
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual(1, manager.CollectedCount, "El contador debe valer 1.");
        Assert.AreEqual(1, requests, "Debe pedirse exactamente un evento de mundo.");
        Assert.IsTrue(bridge.HasRun, "El puente debe haber arrancado.");

        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(bridgePivot.gameObject.activeSelf, "El puente debe estar visible.");
        Assert.AreEqual(1f, bridgePivot.localScale.z, 0.01f, "El puente debe haber terminado de crecer.");
        Assert.IsTrue(fragment == null, "El fragmento recogido debe desaparecer.");

        // Un segundo aviso del mismo fragmento no cuenta ni vuelve a disparar el evento.
        GameEvents.RaiseFragmentCollected(data);
        yield return null;
        Assert.AreEqual(1, manager.CollectedCount, "El mismo fragmento no debe contarse dos veces.");
        Assert.AreEqual(1, requests, "El evento no debe repetirse.");

        GameEvents.WorldEventRequested -= counter;
    }

    [UnityTest]
    public IEnumerator Sin_el_puente_Kael_cae_al_abismo()
    {
        yield return LoadLevel();
        Teleport(new Vector3(0f, 0.05f, 10f));
        yield return new WaitForSeconds(0.3f);

        // Kael cae al abismo (y luego reaparece por la zona de caída): lo que se comprueba es que nunca llegó a la Isla B.
        float maxZ = player.transform.position.z;
        Press(keyboard.wKey);
        float elapsed = 0f;
        while (elapsed < 2.5f)
        {
            elapsed += Time.deltaTime;
            maxZ = Mathf.Max(maxZ, player.transform.position.z);
            yield return null;
        }
        Release(keyboard.wKey);

        Assert.Less(maxZ, 30f, "Sin el puente, Kael no debe poder cruzar al otro lado.");
    }

    [UnityTest]
    public IEnumerator Con_el_puente_Kael_cruza_a_la_Isla_B()
    {
        yield return LoadLevel();
        GameEvents.RaiseWorldEventRequested("puente_1");
        yield return new WaitForSeconds(2.5f);

        Teleport(new Vector3(0f, 0.05f, 11f));
        yield return new WaitForSeconds(0.3f);

        Press(keyboard.wKey);
        yield return new WaitForSeconds(3.5f);
        Release(keyboard.wKey);

        Vector3 position = player.transform.position;
        Assert.Greater(position.z, 31f, "Kael debería haber cruzado el abismo (z > 31).");
        Assert.Greater(position.y, -0.5f, "Kael no debería haberse caído.");
    }
}
