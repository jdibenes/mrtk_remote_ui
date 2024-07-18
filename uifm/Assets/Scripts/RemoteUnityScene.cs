
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Audio;
using UnityEngine.Video;
using System.Runtime.InteropServices;

public class RemoteUnityScene : MonoBehaviour
{
    private const uint ERROR_BASE = 0x80000000;

    public GameObject m_tts;
    public GameObject m_panel_sample;
    public GameObject m_surface_sample;
    public GameObject m_text_sample;
    public GameObject m_button_sample;
    public GameObject m_audio;

    private Dictionary<int, GameObject> m_remote_objects;
    private Dictionary<string, GameObject> m_panel_manifest;
    private bool m_loop;
    private bool m_mode;
    private bool m_debug;
    private int m_last_key;

    [Tooltip("Set to BasicMaterial to support semi-transparent primitives.")]
    public Material m_material;

    void Start()
    {
        hl2ss.UpdateCoordinateSystem();
        hl2ss.Initialize(false, false, false, false, false, false, false, false, true, false, false, false, true);

        m_panel_manifest = new Dictionary<string, GameObject>();
        m_remote_objects = new Dictionary<int, GameObject>();
        m_loop = false;
        m_mode = false;
        m_debug = false;
    }

    void Update()
    {
        while (GetMessage() && m_loop) ;
        DispatchMessage();
    }

    bool GetMessage()
    {
        uint command;
        byte[] data;
        if (!hl2ss.PullMessage(out command, out data)) { return false; }
        uint result;
        try { result = ProcessMessage(command, data); } catch (Exception e) { result = ExceptionVector(e); }
        hl2ss.PushResult(result);
        hl2ss.AcknowledgeMessage(command);
        return true;
    }

    uint ExceptionVector(Exception e)
    {
        if (!m_debug) { return ERROR_BASE; }
        byte[] msg = System.Text.Encoding.UTF8.GetBytes(e.ToString());
        GCHandle h = GCHandle.Alloc(msg, GCHandleType.Pinned);
        hl2ss.PushMessage(0xFFFFFFFE, (uint)msg.Length, h.AddrOfPinnedObject());
        h.Free();
        return ERROR_BASE;
    }

    bool DispatchMessage()
    {
        uint result;
        if (!hl2ss.PullResult(out result)) { return false; }
        hl2ss.AcknowledgeResult(result);
        return true;
    }

    uint ProcessMessage(uint command, byte[] data)
    {
        uint ret = ~0U;

        switch (command)
        {
        // Remote Unity Scene (Legacy) IO Region ------------------------------
        case 0: ret = MSG_CreatePrimitive(data); break;
        case 1: ret = MSG_SetActive(data); break;
        case 2: ret = MSG_SetWorldTransform(data); break;
        case 3: ret = MSG_SetLocalTransform(data); break;
        case 4: ret = MSG_SetColor(data); break;
        case 5: ret = MSG_SetTexture(data); break;
        case 6: ret = MSG_CreateText(data); break;
        case 7: ret = MSG_SetText(data); break;
        case 8: ret = MSG_Say(data); break;
        case 16: ret = MSG_Remove(data); break;
        case 17: ret = MSG_RemoveAll(data); break;
        case 18: ret = MSG_BeginDisplayList(data); break;
        case 19: ret = MSG_EndDisplayList(data); break;
        case 20: ret = MSG_SetTargetMode(data); break;
        case 21: ret = MSG_SetDebugMode(data); break;
        // File IO Region -----------------------------------------------------
        case 32: ret = MSG_FileExists(data); break;
        case 33: ret = MSG_FileUpload(data); break;
        case 34: ret = MSG_FileDelete(data); break;
        case 35: ret = MSG_FileMove(data); break;
        // Panel IO Region ----------------------------------------------------
        case 48: ret = MSG_PanelCreate(data); break;
        case 49: ret = MSG_PanelExists(data); break;
        case 50: ret = MSG_PanelDestroy(data); break;
        case 51: ret = MSG_PanelSetActive(data); break;
        case 52: ret = MSG_PanelSetTransform(data); break;
        // Surface IO Region --------------------------------------------------
        case 64: ret = MSG_SurfaceCreate(data); break;
        case 65: ret = MSG_SurfaceExists(data); break;
        case 66: ret = MSG_SurfaceDestroy(data); break;
        case 67: ret = MSG_SurfaceSetActive(data); break;
        case 68: ret = MSG_SurfaceSetTransform(data); break;
        case 69: ret = MSG_SurfaceSetTextureData(data); break;
        case 70: ret = MSG_SurfaceSetTextureFile(data); break;
        case 71: ret = MSG_SurfaceSetVideoFile(data); break;
        case 72: ret = MSG_SurfaceVideoConfigure(data); break;
        case 73: ret = MSG_SurfaceVideoConfigureAudio(data); break;
        case 74: ret = MSG_SurfaceVideoControl(data); break;
        // Text IO Region -----------------------------------------------------
        case 80: ret = MSG_TextCreate(data); break;
        case 81: ret = MSG_TextExists(data); break;
        case 82: ret = MSG_TextDestroy(data); break;
        case 83: ret = MSG_TextSetActive(data); break;
        case 84: ret = MSG_TextSetTransform(data); break;
        case 85: ret = MSG_TextSetFormat(data); break;
        case 86: ret = MSG_TextSetText(data); break;
        // Button IO Region ---------------------------------------------------
        case 96: ret = MSG_ButtonCreate(data); break;
        case 97: ret = MSG_ButtonExists(data); break;
        case 98: ret = MSG_ButtonDestroy(data); break;
        case 99: ret = MSG_ButtonSetActive(data); break;
        case 100: ret = MSG_ButtonSetTransform(data); break;
        case 101: ret = MSG_ButtonSetText(data); break;
        case 102: ret = MSG_ButtonGetState(data); break;
        // Audio Output IO Region ---------------------------------------------
        case 112: ret = MSG_AudioPlayData(data); break;
        case 113: ret = MSG_AudioPlayFile(data); break;
        case 114: ret = MSG_AudioConfigure(data); break;
        case 115: ret = MSG_AudioControl(data); break;
        // IPC IO Region ------------------------------------------------------
        case ~0U: ret = MSG_Disconnect(data); break;
        default:  throw new Exception(string.Format("Command {0} not implemented!", command));
        }

        return ret;
    }

    //--------------------------------------------------------------------------
    // Remote Unity Scene (Legacy)
    //--------------------------------------------------------------------------

    // OK
    uint AddGameObject(GameObject go)
    {
        int key = go.GetInstanceID();
        m_remote_objects.Add(key, go);
        m_last_key = key;

        return (uint)key;
    }

    // OK
    int GetKey(byte[] data)
    {
        return m_mode ? m_last_key : BitConverter.ToInt32(data, 0);
    }

    // OK
    void UnpackTransform(byte[] data, int offset, out Vector3 position, out Quaternion rotation, out Vector3 locscale)
    {
        float[] f = new float[10];
        for (int i = 0; i < f.Length; ++i) { f[i] = BitConverter.ToSingle(data, offset + (i * 4)); }

        position = new Vector3(f[0], f[1], f[2]);
        rotation = new Quaternion(f[3], f[4], f[5], f[6]);
        locscale = new Vector3(f[7], f[8], f[9]);
    }

    // OK
    uint MSG_Remove(byte[] data)
    {
        if (data.Length < 4) { return 0; }

        GameObject go;
        int key = GetKey(data);
        if (!m_remote_objects.TryGetValue(key, out go)) { return 0; }

        m_remote_objects.Remove(key);
        Destroy(go);

        return 1;
    }

    // OK
    uint MSG_RemoveAll(byte[] data)
    {
        foreach (var go in m_remote_objects.Values) { Destroy(go); }
        m_remote_objects.Clear();
        return 1;
    }

    // OK
    uint MSG_BeginDisplayList(byte[] data)
    {
        m_loop = true;
        return 1;
    }

    // OK
    uint MSG_EndDisplayList(byte[] data)
    {
        m_loop = false;
        return 1;
    }

    // OK
    uint MSG_SetTargetMode(byte[] data)
    {
        if (data.Length < 4) { return 0; }
        m_mode = BitConverter.ToUInt32(data, 0) != 0;
        return 1;
    }

    // OK
    uint MSG_Disconnect(byte[] data)
    {
        m_loop = false;
        m_mode = false;
        m_last_key = 0;

        return ~0U;
    }

    // OK
    uint MSG_CreatePrimitive(byte[] data)
    {
        if (data.Length < 4) { return 0; }

        PrimitiveType t;
        switch (BitConverter.ToUInt32(data, 0))
        {
        case 0: t = PrimitiveType.Sphere; break;
        case 1: t = PrimitiveType.Capsule; break;
        case 2: t = PrimitiveType.Cylinder; break;
        case 3: t = PrimitiveType.Cube; break;
        case 4: t = PrimitiveType.Plane; break;
        default: t = PrimitiveType.Quad; break;
        }

        GameObject go = GameObject.CreatePrimitive(t);

        go.GetComponent<Renderer>().material = m_material;
        go.SetActive(false);

        return AddGameObject(go);
    }

    // OK
    uint MSG_SetActive(byte[] data)
    {
        if (data.Length < 8) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }

        go.SetActive(BitConverter.ToInt32(data, 4) != 0);

        return 1;
    }

    // OK
    uint MSG_SetWorldTransform(byte[] data)
    {
        if (data.Length < 44) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }

        Vector3 position;
        Quaternion rotation;
        Vector3 locscale;

        UnpackTransform(data, 4, out position, out rotation, out locscale);

        go.transform.parent = null;

        go.transform.SetPositionAndRotation(position, rotation);
        go.transform.localScale = locscale;

        return 1;
    }

    // OK
    uint MSG_SetLocalTransform(byte[] data)
    {
        if (data.Length < 44) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }

        Vector3 position;
        Quaternion rotation;
        Vector3 locscale;

        UnpackTransform(data, 4, out position, out rotation, out locscale);

        go.transform.parent = transform;

        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = locscale;

        return 1;
    }

    // OK
    uint MSG_SetColor(byte[] data)
    {
        if (data.Length < 20) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }

        go.GetComponent<Renderer>().material.color = new Color(BitConverter.ToSingle(data, 4), BitConverter.ToSingle(data, 8), BitConverter.ToSingle(data, 12), BitConverter.ToSingle(data, 16));

        return 1;
    }

    // OK
    uint MSG_SetTexture(byte[] data)
    {
        if (data.Length < 4) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }

        Texture2D tex;
        if (data.Length > 4)
        {
            tex = new Texture2D(2, 2);
            byte[] image = new byte[data.Length - 4];
            Array.Copy(data, 4, image, 0, image.Length);
            tex.LoadImage(image);
        }
        else
        {
            tex = null;
        }

        go.GetComponent<Renderer>().material.mainTexture = tex;

        return 1;
    }

    // OK
    uint MSG_CreateText(byte[] data)
    {
        GameObject go = new GameObject();
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();

        go.SetActive(false);

        tmp.enableWordWrapping = false;
        tmp.autoSizeTextContainer = true;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.text = "";

        return AddGameObject(go);
    }

    // OK
    uint MSG_SetText(byte[] data)
    {
        if (data.Length < 24) { return 0; }

        GameObject go;
        if (!m_remote_objects.TryGetValue(GetKey(data), out go)) { return 0; }
        TextMeshPro tmp = go.GetComponent<TextMeshPro>();
        if (tmp == null) { return 0; }

        tmp.fontSize = BitConverter.ToSingle(data, 4);
        tmp.color = new Color(BitConverter.ToSingle(data, 8), BitConverter.ToSingle(data, 12), BitConverter.ToSingle(data, 16), BitConverter.ToSingle(data, 20));

        string str;
        if (data.Length > 24)
        {
            byte[] str_bytes = new byte[data.Length - 24];
            Array.Copy(data, 24, str_bytes, 0, str_bytes.Length);
            try { str = System.Text.Encoding.UTF8.GetString(str_bytes); } catch { return 0; }
        }
        else
        {
            str = "";
        }

        tmp.text = str;

        return 1;
    }

    // OK
    uint MSG_Say(byte[] data)
    {
        m_tts.GetComponent<TextToSpeech>().StartSpeaking(System.Text.Encoding.UTF8.GetString(data));
        return 0;
    }

    // OK
    uint MSG_SetDebugMode(byte[] data)
    {
        m_debug = BitConverter.ToInt32(data, 0) != 0;
        return 0;
    }

    //--------------------------------------------------------------------------
    // UI Functions
    //--------------------------------------------------------------------------

    // OK
    void Panel_Create(string name, float dx, float dy, float dz)
    {
        if (Panel_Exists(name)) { throw new Exception(string.Format("Panel [{0}] already exists!", name)); }
        GameObject panel = Instantiate(m_panel_sample, transform.position + (dx * transform.right + dy * transform.up + dz * transform.forward), transform.rotation);
        m_panel_manifest.Add(name, panel);
        panel.name = name;
    }

    // OK
    bool Panel_Exists(string name)
    {
        return m_panel_manifest.ContainsKey(name);
    }

    // OK
    GameObject Panel_Get(string name)
    {
        return m_panel_manifest[name];
    }

    // OK
    void Panel_Destroy(string name)
    {
        GameObject panel = Panel_Get(name);
        m_panel_manifest.Remove(name);
        Destroy(panel);
    }

    // OK
    void Panel_SetActive(string name, bool active)
    {
        Panel_Get(name).SetActive(active);
    }

    // OK
    void Panel_SetTransform(string name, Vector3 pin_position, Vector3 pan_position, Vector3 pan_scale)
    {
        GameObject panel = Panel_Get(name);

        Transform pin_transform = panel.transform.Find("ButtonPin");
        Transform backplate_transform = panel.transform.Find("Backplate/Quad");
        
        pin_transform.localPosition = pin_position;
        backplate_transform.localPosition = pan_position;
        backplate_transform.localScale = pan_scale;
    }

    // OK
    void Control_Create(string name, string id, GameObject base_object)
    {
        if (Control_Exists(name, id)) { throw new Exception(string.Format("Control [{0}/{1}] already exists!", name, id)); }
        GameObject child = Instantiate(base_object, Panel_Get(name).transform);
        child.name = id;
    }

    // OK
    GameObject Control_Get(string name, string id)
    {
        return Panel_Get(name).transform.Find(id).gameObject;
    }

    // OK
    bool Control_Exists(string name, string id)
    {
        return Panel_Get(name).transform.Find(id) != null;
    }

    // OK
    void Control_Destroy(string name, string id)
    {
        Destroy(Control_Get(name, id));
    }

    // OK
    void Control_SetActive(string name, string id, bool active)
    {
        Control_Get(name, id).SetActive(active);
    }

    // OK
    void Control_SetTransform(string name, string id, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Transform child_transform = Panel_Get(name).transform.Find(id);

        child_transform.localPosition = position;
        child_transform.localRotation = rotation;
        child_transform.localScale = scale;
    }

    // OK
    string GetFullPath(string file_name)
    {
        if (file_name.Contains("/") || file_name.Contains("\\")) { throw new Exception("Path separators are not allowed!"); }
        return Application.persistentDataPath + "/" + file_name;
    }

    // OK
    string UnpackString(byte[] data, int offset, int count)
    {
        return System.Text.Encoding.UTF8.GetString(data, offset, count);
    }

    //--------------------------------------------------------------------------
    // File Operations
    //--------------------------------------------------------------------------

    // OK
    uint MSG_FileExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        string file_name = UnpackString(data, offset_name, offset_end - offset_name);

        return File.Exists(GetFullPath(file_name)) ? 1U : 0;
    }

    // OK
    uint MSG_FileUpload(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string file_name = UnpackString(data, offset_name, offset_data - offset_name);

        using (var fs = new FileStream(GetFullPath(file_name), FileMode.Create, FileAccess.Write)) { fs.Write(data, offset_data, offset_end - offset_data); }

        return 0;
    }

    // OK
    uint MSG_FileDelete(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        string file_name = UnpackString(data, offset_name, offset_end - offset_name);

        File.Delete(GetFullPath(file_name));

        return 0;
    }

    // OK
    uint MSG_FileMove(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_dst  = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string file_name = UnpackString(data, offset_name, offset_dst - offset_name);
        string file_dst  = UnpackString(data, offset_dst,  offset_end - offset_dst);

        File.Move(GetFullPath(file_name), GetFullPath(file_dst));

        return 0;
    }

    //--------------------------------------------------------------------------
    // UI Panel
    //--------------------------------------------------------------------------
    
    // OK
    uint MSG_PanelCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_data - offset_name);

        float dx = BitConverter.ToSingle(data, offset_data + 0);
        float dy = BitConverter.ToSingle(data, offset_data + 4);
        float dz = BitConverter.ToSingle(data, offset_data + 8);

        Panel_Create(name, dx, dy, dz);

        return 0;
    }

    // OK
    uint MSG_PanelExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_end - offset_name);

        return Panel_Exists(name) ? 1U : 0;
    }

    // OK
    uint MSG_PanelDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_end - offset_name);

        Panel_Destroy(name);

        return 0;
    }

    // OK
    uint MSG_PanelSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_data - offset_name);

        bool active = BitConverter.ToInt32(data, offset_data + 0) != 0;

        Panel_SetActive(name, active);

        return 0;
    }

    // OK
    uint MSG_PanelSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_data - offset_name);

        float pin_tx = BitConverter.ToSingle(data, offset_data +  0);
        float pin_ty = BitConverter.ToSingle(data, offset_data +  4);
        float pin_tz = BitConverter.ToSingle(data, offset_data +  8);
        float pan_tx = BitConverter.ToSingle(data, offset_data + 12);
        float pan_ty = BitConverter.ToSingle(data, offset_data + 16);
        float pan_tz = BitConverter.ToSingle(data, offset_data + 20);
        float pan_sx = BitConverter.ToSingle(data, offset_data + 24);
        float pan_sy = BitConverter.ToSingle(data, offset_data + 28);
        float pan_sz = BitConverter.ToSingle(data, offset_data + 32);

        Panel_SetTransform(name, new Vector3(pin_tx, pin_ty, pin_tz), new Vector3(pan_tx, pan_ty, pan_tz), new Vector3(pan_sx, pan_sy, pan_sz));

        return 0;
    }

    //------------------------------------------------------------------------
    // UI Surface
    //------------------------------------------------------------------------

    // OK
    uint MSG_SurfaceCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Create(name, id, m_surface_sample);

        return 0;
    }

    // OK
    uint MSG_SurfaceExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        return Control_Exists(name, id) ? 1U : 0;
    }

    // OK
    uint MSG_SurfaceDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Destroy(name, id);

        return 0;
    }

    // OK
    uint MSG_SurfaceSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        bool active = BitConverter.ToInt32(data, offset_data + 0) != 0;

        Control_SetActive(name, id, active);

        return 0;
    }

    // OK
    uint MSG_SurfaceSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        float px = BitConverter.ToSingle(data, offset_data +  0);
        float py = BitConverter.ToSingle(data, offset_data +  4);
        float pz = BitConverter.ToSingle(data, offset_data +  8);
        float qx = BitConverter.ToSingle(data, offset_data + 12);
        float qy = BitConverter.ToSingle(data, offset_data + 16);
        float qz = BitConverter.ToSingle(data, offset_data + 20);
        float qw = BitConverter.ToSingle(data, offset_data + 24);
        float sx = BitConverter.ToSingle(data, offset_data + 28);
        float sy = BitConverter.ToSingle(data, offset_data + 32);
        float sz = BitConverter.ToSingle(data, offset_data + 36);

        Control_SetTransform(name, id, new Vector3(px, py, pz), new Quaternion(qx, qy, qz, qw), new Vector3(sx, sy, sz));

        return 0;
    }

    // OK
    uint MSG_SurfaceSetTextureData(byte[] data)
    {
        int offset_name   = BitConverter.ToInt32(data, 0);
        int offset_id     = BitConverter.ToInt32(data, 4);
        int offset_data   = BitConverter.ToInt32(data, 8);
        int offset_end    = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        int image_size = offset_end - offset_data;
        byte[] image = new byte[image_size];
        Array.Copy(data, offset_data, image, 0, image_size);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(image);

        Control_Get(name, id).GetComponent<Renderer>().material.mainTexture = tex;

        return 0;
    }

    // OK
    uint MSG_SurfaceSetTextureFile(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name      = UnpackString(data, offset_name, offset_id   - offset_name);
        string id        = UnpackString(data, offset_id,   offset_data - offset_id);
        string file_name = UnpackString(data, offset_data, offset_end  - offset_data);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(GetFullPath(file_name)));

        Control_Get(name, id).GetComponent<Renderer>().material.mainTexture = tex;

        return 0;
    }
    
    // OK
    uint MSG_SurfaceSetVideoFile(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_file = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name      = UnpackString(data, offset_name, offset_id   - offset_name);
        string id        = UnpackString(data, offset_id,   offset_file - offset_id);
        string file_name = UnpackString(data, offset_file, offset_end  - offset_file);

        VideoPlayer video_player = Control_Get(name, id).GetComponent<VideoPlayer>();

        video_player.url = GetFullPath(file_name);

        return 0;
    }

    // OK
    uint MSG_SurfaceVideoConfigure(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        int   key   = BitConverter.ToInt32( data, offset_data + 0);
        float value = BitConverter.ToSingle(data, offset_data + 4);

        VideoPlayer video_player = Control_Get(name, id).GetComponent<VideoPlayer>();

        switch (key)
        {
        case 0:  video_player.isLooping         = value != 0.0f; break;
        case 1:  video_player.skipOnDrop        = value != 0.0f; break;
        case 2:  video_player.waitForFirstFrame = value != 0.0f; break;
        case 3:  video_player.playbackSpeed     = value;         break;
        default: throw new Exception("Unknown Video Option");
        }

        return 0;
    }

    // OK
    uint MSG_SurfaceVideoConfigureAudio(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);        

        int   key   = BitConverter.ToInt32( data, offset_data + 0);
        int   index = BitConverter.ToInt32( data, offset_data + 4);
        float value = BitConverter.ToSingle(data, offset_data + 8);

        VideoPlayer video_player = Control_Get(name, id).GetComponent<VideoPlayer>();

        switch (key)
        {
        case 0:  video_player.SetDirectAudioMute(  (ushort)index, value != 0.0f); break;
        case 1:  video_player.SetDirectAudioVolume((ushort)index, value);         break;
        default: throw new Exception("Unknown Video Audio Option");
        }

        return 0;
    }

    // OK
    uint MSG_SurfaceVideoControl(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_key  = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_key - offset_id);

        int key = BitConverter.ToInt32(data, offset_key + 0);

        VideoPlayer video_player = Control_Get(name, id).GetComponent<VideoPlayer>();        

        switch (key)
        {
        case 0:  video_player.Play();  break;
        case 1:  video_player.Pause(); break;
        case 2:  video_player.Stop();  break;
        case 3:  return video_player.isPlaying ? 1U : 0;
        case 4:  return video_player.isPaused  ? 1U : 0;
        default: throw new Exception("Unknown Video Operation");
        }

        return 0;
    }

    //------------------------------------------------------------------------
    // UI Text
    //------------------------------------------------------------------------

    // OK
    uint MSG_TextCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Create(name, id, m_text_sample);

        return 0;
    }

    // OK
    uint MSG_TextExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        return Control_Exists(name, id) ? 1U : 0;
    }

    // OK
    uint MSG_TextDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Destroy(name, id);

        return 0;
    }

    // OK
    uint MSG_TextSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        bool active = BitConverter.ToInt32(data, offset_data + 0) != 0;

        Control_SetActive(name, id, active);

        return 0;
    }

    // OK
    uint MSG_TextSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        float px = BitConverter.ToSingle(data, offset_data +  0);
        float py = BitConverter.ToSingle(data, offset_data +  4);
        float pz = BitConverter.ToSingle(data, offset_data +  8);
        float w  = BitConverter.ToSingle(data, offset_data + 12);
        float h  = BitConverter.ToSingle(data, offset_data + 16);

        RectTransform rt = Control_Get(name, id).GetComponent<RectTransform>();

        rt.localPosition = new Vector3(px, py, pz);
        rt.sizeDelta     = new Vector2(w, h);

        return 0;
    }

    // OK
    uint MSG_TextSetFormat(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        int   font_style  = BitConverter.ToInt32( data, offset_data +  0);
        float font_size   = BitConverter.ToSingle(data, offset_data +  4);
        bool  auto_size   = BitConverter.ToSingle(data, offset_data +  8) != 0;
        float color_r     = BitConverter.ToSingle(data, offset_data + 12);
        float color_g     = BitConverter.ToSingle(data, offset_data + 16);
        float color_b     = BitConverter.ToSingle(data, offset_data + 20);
        float color_a     = BitConverter.ToSingle(data, offset_data + 24);
        int   h_alignment = BitConverter.ToInt32( data, offset_data + 28);
        int   v_alignment = BitConverter.ToInt32( data, offset_data + 32);
        bool  wrap        = BitConverter.ToInt32( data, offset_data + 36) != 0;
        int   overflow    = BitConverter.ToInt32( data, offset_data + 40);

        TextMeshPro tmp = Control_Get(name, id).GetComponent<TextMeshPro>();

        tmp.fontStyle           = (FontStyles)font_style;
        tmp.fontSize            = font_size;
        tmp.enableAutoSizing    = auto_size;
        tmp.color               = new Color(color_r, color_g, color_b, color_a);
        tmp.horizontalAlignment = (HorizontalAlignmentOptions)h_alignment;
        tmp.verticalAlignment   = (VerticalAlignmentOptions)v_alignment;
        tmp.enableWordWrapping  = wrap;
        tmp.overflowMode        = (TextOverflowModes)overflow;

        return 0;
    }

    // OK
    uint MSG_TextSetText(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);
        string text = UnpackString(data, offset_data, offset_end  - offset_data);

        TextMeshPro tmp = Control_Get(name, id).GetComponent<TextMeshPro>();
        tmp.text = text;

        return 0;
    }

    //------------------------------------------------------------------------
    // UI Button
    //------------------------------------------------------------------------

    // OK
    uint MSG_ButtonCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Create(name, id, m_button_sample);

        ButtonEvent be = Control_Get(name, id).GetComponent<ButtonEvent>();
        be.pressed = false;

        return 0;
    }

    // OK
    uint MSG_ButtonExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        return Control_Exists(name, id) ? 1U : 0;
    }

    // OK
    uint MSG_ButtonDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        Control_Destroy(name, id);

        return 0;
    }

    // OK
    uint MSG_ButtonSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        bool active = BitConverter.ToInt32(data, offset_data + 0) != 0;

        Control_Get(name, id).SetActive(active);

        return 0;
    }

    // OK
    uint MSG_ButtonSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);

        float px = BitConverter.ToSingle(data, offset_data +  0);
        float py = BitConverter.ToSingle(data, offset_data +  4);
        float pz = BitConverter.ToSingle(data, offset_data +  8);
        float qx = BitConverter.ToSingle(data, offset_data + 12);
        float qy = BitConverter.ToSingle(data, offset_data + 16);
        float qz = BitConverter.ToSingle(data, offset_data + 20);
        float qw = BitConverter.ToSingle(data, offset_data + 24);
        float sx = BitConverter.ToSingle(data, offset_data + 28);
        float sy = BitConverter.ToSingle(data, offset_data + 32);
        float sz = BitConverter.ToSingle(data, offset_data + 36);

        Control_SetTransform(name, id, new Vector3(px, py, pz), new Quaternion(qx, qy, qz, qw), new Vector3(sx, sy, sz));

        return 0;
    }

    // OK
    uint MSG_ButtonSetText(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id   - offset_name);
        string id   = UnpackString(data, offset_id,   offset_data - offset_id);
        string text = UnpackString(data, offset_data, offset_end  - offset_data);

        TextMeshPro tmp = Panel_Get(name).transform.Find(id).Find("IconAndText").Find("TextMeshPro").gameObject.GetComponent<TextMeshPro>();
        tmp.text = text;

        return 0;
    }

    // OK
    uint MSG_ButtonGetState(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id   = BitConverter.ToInt32(data, 4);
        int offset_end  = data.Length;

        string name = UnpackString(data, offset_name, offset_id  - offset_name);
        string id   = UnpackString(data, offset_id,   offset_end - offset_id);

        ButtonEvent be = Control_Get(name, id).GetComponent<ButtonEvent>();
        bool pressed = be.pressed;
        be.pressed = false;

        return pressed ? 1U : 0;
    }

    //--------------------------------------------------------------------------
    // Audio Output
    //--------------------------------------------------------------------------

    // OK
    uint MSG_AudioPlayData(byte[] data)
    {
        int offset_name   = BitConverter.ToInt32(data, 0);
        int offset_format = BitConverter.ToInt32(data, 4);
        int offset_data   = BitConverter.ToInt32(data, 8);
        int offset_end    = data.Length;

        string name = UnpackString(data, offset_name, offset_format - offset_name);

        int channels  = BitConverter.ToInt32(data, offset_format + 0);
        int frequency = BitConverter.ToInt32(data, offset_format + 4);

        int data_size = offset_end - offset_data;
        int sample_count = data_size / (channels * sizeof(float));

        AudioClip audio_clip = AudioClip.Create(name, sample_count, channels, frequency, false);
        float[] samples = new float[data_size / sizeof(float)];
        Buffer.BlockCopy(data, offset_data, samples, 0, data_size);
        audio_clip.SetData(samples, 0);

        AudioSource source = m_audio.GetComponent<AudioSource>();
        source.PlayOneShot(audio_clip);

        return 0;
    }

    // OK
    uint MSG_AudioPlayFile(byte[] data)
    {
        int offset_name   = BitConverter.ToInt32(data, 0);
        int offset_format = BitConverter.ToInt32(data, 4);
        int offset_end    = data.Length;

        string file_name = UnpackString(data, offset_name, offset_format - offset_name);

        int audio_type = BitConverter.ToInt32(data, offset_format + 0);

        AudioSource source = m_audio.GetComponent<AudioSource>();
        source.PlayOneShot((new WWW(GetFullPath(file_name))).GetAudioClip(false, true, (AudioType)audio_type));

        return 0;
    }

    // OK
    uint MSG_AudioConfigure(byte[] data)
    {
        int offset_data = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        int   key   = BitConverter.ToInt32( data, offset_data + 0);
        float value = BitConverter.ToSingle(data, offset_data + 4);

        AudioSource source = m_audio.GetComponent<AudioSource>();

        switch (key)
        {
        case 0:  source.panStereo = value; break;
        case 1:  source.pitch     = value; break;
        case 2:  source.volume    = value; break;
        default: throw new Exception("Unknown Audio Option");
        }

        return 0;
    }

    // OK
    uint MSG_AudioControl(byte[] data)
    {
        int offset_data = BitConverter.ToInt32(data, 0);
        int offset_end  = data.Length;

        int key = BitConverter.ToInt32(data, offset_data + 0);

        AudioSource source = m_audio.GetComponent<AudioSource>();

        switch (key)
        {
        case 0:  source.mute = true;  break;
        case 1:  source.mute = false; break;
        case 2:  source.Pause();      break;
        case 3:  source.UnPause();    break;
        case 4:  source.Stop();       break;
        case 5:  return source.isPlaying ? 1U : 0;
        default: throw new Exception("Unknown Audio Operation");
        }

        return 0;
    }
}
