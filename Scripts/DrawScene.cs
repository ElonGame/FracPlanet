﻿using UnityEngine;
// Draw or destroy world objects, excluding controller menus.
public class DrawScene : MonoBehaviour {
    // the WandController object for the right controller.
    private WandController wand;
    private MainLight aMainLight;
    // A line drawn from the controller.
    public LineRenderer pointerLineRenderer;
    private Vector3[] lineRendererVertices = new Vector3[2];
    public GameObject pointerLine;
    private Color baseColor;
    private Color hitColor;
    public string onWhichPlanet = "";
    // teleporting stuff.
    private bool teleporting, teleportHome;
    private Vector3 startPos;
    private RaycastHit hit, hit2;
    private float teleDistance;
    private Teleport teleport; // teleport library.
    // screen fader.
    private ScreenFader screenFade;
    private DrawScene aScene;
    private SkyBoxManager skybox;
    private float timer = 0;
    private float timerMax = 0;
    // planet related stuff.
    private PlanetManager planetManager;
    private ScanBox scanBox;
    // 1000 queued up seeds for our planets.
    private int[] seedQueue = new int[1500];
    private int seedQueueIndex = 750;
    public int planetSeed;
    public bool havePlanet = false; // are we rendering a planet?
    System.Random rnd;

    void Start () {
        planetManager = gameObject.AddComponent<PlanetManager>();
        wand = GameObject.Find("Controller (right)").GetComponent<WandController>();
        screenFade = GameObject.Find("ScreenFader").GetComponent<ScreenFader>();
        aMainLight = GameObject.Find("Main Light").GetComponent<MainLight>();
        scanBox = gameObject.AddComponent<ScanBox>();
        skybox = gameObject.AddComponent<SkyBoxManager>();

        screenFade.fadeTime = .1F;
        screenFade.enabled = true;
        rnd = new System.Random();
        // queue up 1000 random planet seeds.
        for (int i = 0; i <= seedQueue.Length -1; i++ ) {
            seedQueue[i] = rnd.Next(0, 32000);
        }
    }

    private void Update() {
        if (wand == null) { return;  }
        if (onWhichPlanet.Contains("Planet")) {
            scanBox.HideScanBox();
            teleDistance = 800;
            aMainLight.Disable();
            skybox.setSkyOnPlanet(planetManager.curPlanetType, planetManager.curPlanetSeed, planetManager.planetDiameter);
            return;
        }
        if ((havePlanet) && (!onWhichPlanet.Contains("Planet"))) {
            UpdateScanBox();
            teleDistance = 4500F;
            aMainLight.Enable();
        }
        if ((!havePlanet) && (wand.radialMenu.curMenuType != ("Planet Menu - Child"))) {
            HideScanBox();
            teleDistance = 4500F;
            aMainLight.Enable();
        }
        if ((!havePlanet) && (wand.radialMenu.curMenuType == ("Planet Menu - Child"))) {
            planetManager.UpdatePotentialPlanet(seedQueue[seedQueueIndex]);
            UpdateScanBox(true);
            teleDistance = 4500F;
            aMainLight.Enable();
        }
        skybox.setSkyOffPlanet();
    }

    public void TeleportFade() {
        // control the teleport fader, called every update from wandcontroller.
        if (!teleporting) return;
        screenFade.fadeIn = false; 
        if (!Waited(.3F)) return;
        DoTeleport(teleportHome);
        screenFade.fadeIn = true;
        teleporting = false;
        teleportHome = false;
        return;
    }

    public void Teleport(bool toHome = false) {
        // kick off a teleport sequence, set teleporting to true.
        teleporting = true;
        if (toHome) { teleportHome = true;  return; }
        startPos = wand.transform.position;
    }
    
    private void DoTeleport(bool toHome) {
        string[] planetParts = { "aPlanetCloud", "aPlanet" };
        if (toHome) {
            wand.transform.parent.eulerAngles = new Vector3(0F, 0F, 0F);
            wand.transform.parent.position = new Vector3(0F, 0F, 0F);
            if (GameObject.Find("aPlanet")) { planetManager.ManageOcean(false); }
            // reset terrain and clouds to upright.
            foreach (string planetPart in planetParts) {
                if (GameObject.Find(planetPart)) {
                    GameObject.Find(planetPart).transform.eulerAngles = new Vector3(0, 0, 0);
                }
            }
            onWhichPlanet = "";
            return;
        }
        if (Physics.Raycast(startPos, wand.transform.forward, out hit, teleDistance)) {
            if (hit.transform.gameObject.name.Contains("Planet")) {
                planetManager.ManageOcean(true);
                // if we hit a planet, first rotate the point on the planet we hit to the top.
                // set the hit point (fromPoint) relative to 0,0,0 by subtracting the planet's position.
                Vector3 toPoint = Vector3.up;
                Vector3 fromPoint = hit.point - hit.transform.gameObject.transform.position;
                    Vector3 dropPoint = new Vector3(0, 20000, 3500);
                if (Physics.Raycast(dropPoint, Vector3.down, out hit2, 20000)) {
                    toPoint = hit2.point - hit.transform.gameObject.transform.position;
                }
                // forumulate a quaternion rotation using FromToRotation.
                Quaternion rotateToTop = Quaternion.FromToRotation(fromPoint, toPoint);
                // roate each part of the planet using the caclulated rotation
                foreach (string planetPart in planetParts) {
                    if (GameObject.Find(planetPart)) {
                        GameObject.Find(planetPart).transform.localRotation = rotateToTop * GameObject.Find(planetPart).transform.localRotation;
                    }
                }
                // cast a ray down, and wherever that hits, that's where the player position should be.
                if (onWhichPlanet.Contains("Planet")) {
                    dropPoint = new Vector3(0, wand.transform.parent.position.y, wand.transform.parent.position.z) + new Vector3(0, 100, 0);
                }
                if (Physics.Raycast(dropPoint, Vector3.down, out hit2, 20000)) {
                    wand.transform.parent.position = hit2.point;
                }
            }
            else {
                wand.transform.parent.eulerAngles = new Vector3(0F, 0F, 0F);
                wand.transform.parent.position = hit.point;
                if (GameObject.Find("aPlanet")) { planetManager.ManageOcean(false); }
            }
            onWhichPlanet = hit.transform.gameObject.name;
        }
        PausePlanet();
    }

    private void DoTeleport2(bool toHome) {
        string[] planetParts = { "aPlanetCloud", "aPlanet" };
        if (toHome) {
            wand.transform.parent.eulerAngles = new Vector3(0F, 0F, 0F);
            wand.transform.parent.position = new Vector3(0F, 0F, 0F);
            if (GameObject.Find("aPlanet")) { planetManager.ManageOcean(false); }
            // reset terrain and clouds to upright.
            foreach (string planetPart in planetParts) {
                if (GameObject.Find(planetPart)) {
                    GameObject.Find(planetPart).transform.eulerAngles = new Vector3(0, 0, 0);
                }
            }
            onWhichPlanet = "";
            return;
        }
        if (Physics.Raycast(startPos, wand.transform.forward, out hit, teleDistance)) {
            if (hit.transform.gameObject.name.Contains("Planet")) {
                planetManager.ManageOcean(true);
                // if we hit a planet, first rotate the point on the planet we hit to the top.
                // set the hit point (fromPoint) relative to 0,0,0 by subtracting the planet's position.
                Vector3 fromPoint = hit.point - hit.transform.gameObject.transform.position;
                Vector3 toPoint = Vector3.up;
                Vector3 dropPoint = new Vector3(0, 20000, 3500);
                // forumulate a rotation using FromToRotation.
                if (onWhichPlanet.Contains("Planet")) {
                    dropPoint = new Vector3(0, wand.transform.parent.position.y, wand.transform.parent.position.z) + new Vector3(0, 100, 0);
                    // calculate the toPoint by dropping a ray from the CameraRig's local position.
                    if (Physics.Raycast(dropPoint, Vector3.down, out hit2, 20000)) {
                        toPoint = hit2.point - hit.transform.gameObject.transform.position;
                    }
                }
                Debug.Log("fromPoint: " + (fromPoint - hit.transform.gameObject.transform.position));
                Debug.Log("toPoint: " + toPoint);
                Quaternion rotateToTop = Quaternion.FromToRotation(fromPoint, toPoint);
                // finally, roate each part of the planet using the caclulated rotation.
                foreach (string planetPart in planetParts) {
                    if (GameObject.Find(planetPart)) {
                        GameObject.Find(planetPart).transform.localRotation *= rotateToTop;
                    }
                }
                // cast a ray down, and wherever that hits, that's where the player position should be.
                if (onWhichPlanet.Contains("Planet")) {
                    dropPoint = new Vector3(0, wand.transform.parent.position.y, wand.transform.parent.position.z) + new Vector3(0, 100, 0);
                }
                if (Physics.Raycast(dropPoint, Vector3.down, out hit2, 20000)) {
                    wand.transform.parent.position = hit2.point;
                }
            }
            else {
                wand.transform.parent.eulerAngles = new Vector3(0F, 0F, 0F);
                wand.transform.parent.position = hit.point;
                if (GameObject.Find("aPlanet")) { planetManager.ManageOcean(false); }
            }
            onWhichPlanet = hit.transform.gameObject.name;
        }
        PausePlanet();
    }

    private bool Waited(float seconds) {
        timerMax = seconds;
        timer += Time.deltaTime;
        if (timer >= timerMax) {
            timer = 0; timerMax = 0;
            return true;
        }
        return false;
    }

    public void DestroyPointerLine() {
        if (pointerLineRenderer != null) {
            pointerLineRenderer = null;
            Destroy(pointerLine);
         }
    }

    public void AddPointerLine(Color newBaseColor, Color newHitColor) {
        if (pointerLineRenderer != null) {
            return;
        }
        baseColor = newBaseColor;
        hitColor = newHitColor;
        pointerLine = new GameObject("Pointer Line");
        pointerLineRenderer = pointerLine.AddComponent<LineRenderer>();
        pointerLineRenderer.material = new Material(Shader.Find("Particles/Additive"));
        pointerLineRenderer.startWidth = 0.01f;
        pointerLineRenderer.endWidth = 0.01f;
        pointerLineRenderer.numPositions = 2;
    }

    public void UpdatePointerLine() {
        // Update our LineRenderer
        if (pointerLineRenderer && pointerLineRenderer.enabled) {
            Vector3 startPos = wand.transform.position;
            // If our raycast hits, end the line at that positon. Otherwise,
            // make our line point straight out for 5000 meters.
            // If the raycast hits, the line will be green, if not, red.
            if (Physics.Raycast(startPos, wand.transform.forward, out hit, teleDistance)) {
                lineRendererVertices[1] = hit.point;
                pointerLineRenderer.startColor = baseColor;
                pointerLineRenderer.endColor = baseColor;
            }
            else {
                lineRendererVertices[1] = startPos + wand.transform.forward * teleDistance;
                pointerLineRenderer.startColor = hitColor;
                pointerLineRenderer.endColor = hitColor;
            }

            lineRendererVertices[0] = wand.transform.position;
            pointerLineRenderer.SetPositions(lineRendererVertices);
        }
    }

    public void AddPlanet() {
        planetManager.AddPlanet();
        havePlanet = true;
    }

    public void PausePlanet() {
        // "pause" this planet if we've teleported to it.
        // "unpause" all the other planets.
        planetManager.PausePlanet(onWhichPlanet);
    }

    public void DestroyPlanets() {
        planetManager.DestroyPlanet();
        scanBox.HideScanBox();
        skybox.setSkyOffPlanet();
        onWhichPlanet = "";
        havePlanet = false;
    }

    public void SetPlanetType(string planetType) {
        planetManager.curPlanetType = planetType;
    }

    public void AddPlanetOutline() {
        planetManager.AddPlanetOutline();
    }

    public void DestroyPlanetOutline() {
        planetManager.DestroyPlanetOutline();
    }

    public void UpdateScanBox(bool unknowns = false) {
        string scanBoxText;
        if (unknowns) {
            scanBoxText = planetManager.planetMetaData.getData(true);
        }
        else {
            scanBoxText = planetManager.planetMetaData.getData();
        }
        scanBox.DisplayScanBox(scanBoxText + " " + seedQueueIndex);
    }

    public void HideScanBox() {
        scanBox.HideScanBox();
    }

    public void UpdatePlanets() {
        if (wand == null) { return; }
        string switch_string = wand.radialMenu.whatIsSelected;
        switch (switch_string) {
            case "Next":
                seedQueueIndex += 1;
                if (seedQueueIndex > seedQueue.Length - 1) { seedQueueIndex = 0; }
                planetSeed = seedQueue[seedQueueIndex];
                rnd = new System.Random(planetSeed);
                planetManager.planetDiameter = rnd.Next(500, 5000);
                wand.radialMenu.whatIsSelected = "";
                break;
            case "Previous":
                seedQueueIndex -= 1;
                if (seedQueueIndex < 0) { seedQueueIndex = seedQueue.Length -1; }
                planetSeed = seedQueue[seedQueueIndex];
                rnd = new System.Random(planetSeed);
                planetManager.planetDiameter = rnd.Next(500, 5000);
                wand.radialMenu.whatIsSelected = "";
                break;
            default:
                break;
        }
    }
}
