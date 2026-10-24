using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// QUÉ HACE
///   Comprueba la segunda mitad del nivel 1 (fragmentos 4 a 7): compuerta, segundo puente, plataformas
///   giratorias, restauración del Núcleo, portal sellado/abierto y pantalla final. También que las
///   plataformas alineadas se pueden recorrer saltando (subida y brecha dentro de lo que alcanza Kael).
///
/// POR QUÉ EXISTE
///   Es la evidencia automática de que el nivel se puede terminar de principio a fin y de que cada evento
///   ocurre una sola vez.
///
/// CÓMO SE USA
///   Test Runner > PlayMode, después de Fragmento > Construir Escena. Las pruebas teletransportan a Kael
///   entre islas para no repetir el recorrido de H1/H2, pero cruzan caminando cada obstáculo nuevo.
/// </summary>
public class CompleteLevelH3Tests : LevelTestBase
{
    // ------------------------------------------------------------------ Diseño

    [UnityTest]
    public IEnumerator Los_siete_fragmentos_existen_y_ninguno_flota_fuera_de_una_isla()
    {
        yield return LoadLevel();

        for (int id = 1; id <= 7; id++)
        {
            Fragment fragment = FindFragment(id);
            Assert.IsNotNull(fragment, "Falta el fragmento " + id);

            bool groundBelow = Physics.Raycast(fragment.transform.position, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore);
            Assert.IsTrue(groundBelow, "El fragmento " + id + " no tiene suelo a menos de 3 m: sería inalcanzable.");
        }
    }

    // ------------------------------------------------------------------ Fragmento 4: compuerta

    [UnityTest]
    public IEnumerator El_fragmento_4_abre_la_compuerta_y_Kael_pasa_al_norte()
    {
        yield return LoadLevel();

        DoorOpen door = Object.FindFirstObjectByType<DoorOpen>();
        Assert.IsNotNull(door);
        Assert.IsFalse(door.IsOpen);

        // Cerrada: Kael no pasa. (z = 96 queda a 3 m del fragmento 4: no lo recoge sin querer.)
        Teleport(new Vector3(43f, 5.05f, 96f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(keyboard.wKey);
        Assert.Less(player.transform.position.z, 99f, "Con la compuerta cerrada Kael no debe pasar al norte.");

        CollectFragment(4);
        yield return WaitForControl(3f);
        Assert.IsTrue(door.IsOpen, "La compuerta debe haberse abierto.");

        Teleport(new Vector3(43f, 5.05f, 96f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(3f);
        Release(keyboard.wKey);
        Assert.Greater(player.transform.position.z, 101f, "Kael debería haber cruzado la compuerta hacia el pasillo.");
        Assert.That(player.transform.position.y, Is.InRange(4.9f, 5.3f));
    }

    // ------------------------------------------------------------------ Fragmento 5: segundo puente

    [UnityTest]
    public IEnumerator El_fragmento_5_hace_aparecer_el_puente_de_E_a_F()
    {
        yield return LoadLevel();

        BridgeAppear bridge5 = FindBridge("Puente de luz 5");
        Assert.IsNotNull(bridge5);
        Assert.IsFalse(bridge5.transform.Find("Pivote").gameObject.activeSelf, "El puente 5 no debe verse antes del fragmento.");

        CollectFragment(5);
        yield return WaitForControl(3f);

        Teleport(new Vector3(36f, 5.05f, 125f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.aKey); // la cámara mira al norte: A va hacia -X (al oeste, hacia F)
        yield return new WaitForSeconds(4f);
        Release(keyboard.aKey);

        Assert.Less(player.transform.position.x, 12f, "Kael debería haber cruzado a la isla F (x < 12).");
        Assert.That(player.transform.position.y, Is.InRange(4.9f, 5.3f), "Kael no debería haberse caído.");
    }

    // ------------------------------------------------------------------ Fragmento 6: plataformas giratorias

    [UnityTest]
    public IEnumerator Las_plataformas_giratorias_forman_un_camino_que_Kael_puede_saltar()
    {
        yield return LoadLevel();

        RotatingPlatformsAlign align = Object.FindFirstObjectByType<RotatingPlatformsAlign>();
        Assert.IsNotNull(align);
        Assert.IsFalse(align.IsAligned);

        CollectFragment(6);
        yield return WaitForControl(5f);
        Assert.IsTrue(align.IsAligned, "Las plataformas deben haberse alineado.");

        // Geometría: de la isla F (techo y = 5) a las 4 plataformas y a la cima G (techo y = 12.5).
        List<Bounds> steps = new List<Bounds>();
        foreach (Transform pivot in align.transform)
        {
            Collider platformCollider = pivot.GetComponentInChildren<Collider>();
            if (platformCollider != null)
            {
                steps.Add(platformCollider.bounds);
            }
        }
        Assert.AreEqual(4, steps.Count);
        steps.Sort((a, b) => a.center.z.CompareTo(b.center.z));

        float previousTop = 5f;
        float previousEdgeZ = 136f; // borde norte de la isla F
        foreach (Bounds step in steps)
        {
            Assert.LessOrEqual(step.max.y - previousTop, 1.55f, "Subida demasiado alta para el salto de Kael (1.8 m).");
            Assert.Greater(step.max.y, previousTop - 0.1f);
            Assert.LessOrEqual(step.min.z - previousEdgeZ, 3.2f, "Brecha demasiado larga para el salto.");
            previousTop = step.max.y;
            previousEdgeZ = step.max.z;
        }
        Assert.LessOrEqual(12.5f - previousTop, 1.55f, "Subida final a la cima demasiado alta.");
        Assert.LessOrEqual(167f - previousEdgeZ, 3.2f, "Brecha final a la cima demasiado larga.");

        // Recorrido real: corre hacia el norte y salta cada vez que toca suelo.
        Teleport(new Vector3(2f, 5.05f, 131f));
        yield return new WaitForSeconds(0.4f);
        Press(keyboard.wKey);
        float timeout = 14f;
        while (player.transform.position.z < 170f && player.transform.position.y > 4f && timeout > 0f)
        {
            // Salta solo al llegar al borde (no hay suelo 0.8 m más adelante), como haría una persona.
            Vector3 ahead = player.transform.position + Vector3.forward * 0.8f + Vector3.up * 1f;
            bool edgeAhead = !Physics.Raycast(ahead, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
            if (player.IsGrounded && edgeAhead)
            {
                // Se mantiene el salto hasta pasar el punto más alto (soltar antes acorta el salto).
                Press(keyboard.spaceKey);
                yield return new WaitForSeconds(0.45f);
                Release(keyboard.spaceKey);
                timeout -= 0.45f;
            }
            timeout -= Time.deltaTime;
            yield return null;
        }
        Release(keyboard.wKey);
        Release(keyboard.spaceKey);

        Assert.Greater(player.transform.position.z, 168f, "Kael no logró llegar a la cima saltando por las plataformas.");
        Assert.Greater(player.transform.position.y, 12f, "Kael debería estar en la cima (y ≈ 12.5).");
    }

    // ------------------------------------------------------------------ Fragmento 7: Núcleo y portal

    [UnityTest]
    public IEnumerator El_portal_esta_sellado_hasta_restaurar_el_Nucleo()
    {
        yield return LoadLevel();

        ExitPortal portal = Object.FindFirstObjectByType<ExitPortal>();
        Assert.IsNotNull(portal);
        Assert.IsFalse(portal.IsOpen);

        bool completed = false;
        System.Action onCompleted = () => completed = true;
        GameEvents.LevelCompleted += onCompleted;

        Teleport(new Vector3(2f, 12.55f, 182f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(1.5f);
        Release(keyboard.wKey);
        GameEvents.LevelCompleted -= onCompleted;

        Assert.IsFalse(completed, "Con el portal sellado no se puede completar el nivel.");
        Assert.Less(player.transform.position.z, 185.5f, "El disco sellado debe bloquear a Kael.");
    }

    [UnityTest]
    public IEnumerator Recoger_los_siete_fragmentos_restaura_el_Nucleo_abre_el_portal_y_termina_el_nivel()
    {
        yield return LoadLevel();

        var finished = new List<string>();
        System.Action<string> onFinished = id => finished.Add(id);
        GameEvents.WorldEventFinished += onFinished;

        // Los fragmentos 1-6 se registran sin caminar (ya se prueban uno a uno); el 7 se recoge en la cima.
        for (int id = 1; id <= 6; id++)
        {
            GameEvents.RaiseFragmentCollected(FindFragment(id).Data);
        }
        yield return WaitForControl(8f);

        CoreRestoration core = Object.FindFirstObjectByType<CoreRestoration>();
        ExitPortal portal = Object.FindFirstObjectByType<ExitPortal>();
        Assert.IsFalse(core.IsRestored, "El Núcleo no debe restaurarse antes del fragmento 7.");
        Assert.IsFalse(portal.IsOpen);

        CollectFragment(7);
        yield return WaitForControl(9f);

        Assert.IsTrue(core.IsRestored, "El Núcleo debe haberse restaurado.");
        Assert.IsTrue(portal.IsOpen, "El portal debe estar abierto.");
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        StringAssert.Contains("7 / 7", hud.CounterLabel);

        bool completed = false;
        System.Action onCompleted = () => completed = true;
        GameEvents.LevelCompleted += onCompleted;

        Teleport(new Vector3(2f, 12.55f, 181f));
        yield return new WaitForSeconds(0.3f);
        Press(keyboard.wKey);
        yield return new WaitForSeconds(2f);
        Release(keyboard.wKey);
        GameEvents.LevelCompleted -= onCompleted;
        GameEvents.WorldEventFinished -= onFinished;

        Assert.IsTrue(completed, "Cruzar el portal abierto debe completar el nivel.");
        EndScreen endScreen = Object.FindFirstObjectByType<EndScreen>();
        yield return new WaitForSeconds(2f);
        Assert.IsTrue(endScreen.IsShown, "Debe verse la pantalla 'Región restaurada'.");

        CollectionAssert.AreEquivalent(
            new List<string> { "puente_1", "escalera_2", "plataforma_3", "puerta_4", "puente_5", "plataformas_6", "nucleo_7" },
            finished, "Deben haber terminado exactamente los siete eventos, cada uno una vez.");
    }

}
