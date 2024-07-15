
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Audio;

public class RemoteUnityScene : MonoBehaviour
{
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
    }

    void Update()
    {
        while (GetMessage() && m_loop);
    }

    bool GetMessage()
    {
        uint command;
        byte[] data;
        if (!hl2ss.PullMessage(out command, out data)) { return false; }
        uint result;
        try { result = ProcessMessage(command, data); } catch { result = 0x80000000; }
        hl2ss.PushResult(result);
        hl2ss.AcknowledgeMessage(command);
        return true;
    }

    uint ProcessMessage(uint command, byte[] data)
    {
        uint ret = ~0U;

        switch (command)
        {
        case   0: ret = MSG_CreatePrimitive(data);   break;
        case   1: ret = MSG_SetActive(data);         break;
        case   2: ret = MSG_SetWorldTransform(data); break;
        case   3: ret = MSG_SetLocalTransform(data); break;
        case   4: ret = MSG_SetColor(data);          break;
        case   5: ret = MSG_SetTexture(data);        break;
        case   6: ret = MSG_CreateText(data);        break;
        case   7: ret = MSG_SetText(data);           break;
        case   8: ret = MSG_Say(data);               break; 

        case  16: ret = MSG_Remove(data);            break;
        case  17: ret = MSG_RemoveAll(data);         break;
        case  18: ret = MSG_BeginDisplayList(data);  break;
        case  19: ret = MSG_EndDisplayList(data);    break;
        case  20: ret = MSG_SetTargetMode(data);     break;


        case  32: ret = MSG_FileExists(data);        break;
        case  33: ret = MSG_FileUpload(data);        break;

        case  48: ret = MSG_PanelCreate(data);       break;
        case  49: ret = MSG_PanelExists(data);       break;
        case  50: ret = MSG_PanelDestroy(data);      break;
        case  51: ret = MSG_PanelSetActive(data);    break;
        case  52: ret = MSG_PanelSetTransform(data); break;

        case 64: ret = MSG_SurfaceCreate(data);      break;
        case 65: ret = MSG_SurfaceExists(data); break;
        case 66: ret = MSG_SurfaceDestroy(data); break;
        case 67: ret = MSG_SurfaceSetActive(data); break;
        case 68: ret = MSG_SurfaceSetTransform(data); break;
        case 69: ret = MSG_SurfaceSetTexture(data); break;
        case 70: ret = MSG_SurfaceSetVideo(data); break;

        case 80: ret = MSG_TextCreate(data); break;
        case 81: ret = MSG_TextExists(data); break;
        case 82: ret = MSG_TextDestroy(data); break;
        case 83: ret = MSG_TextSetActive(data); break;
        case 84: ret = MSG_TextSetTransform(data); break;
        case 85: ret = MSG_TextSetFormat(data); break;
        case 86: ret = MSG_TextSetText(data); break;

        case 96: ret = MSG_ButtonCreate(data); break;
        case 97: ret = MSG_ButtonExists(data); break;
        case 98: ret = MSG_ButtonDestroy(data); break;
        case 99: ret = MSG_ButtonSetActive(data); break;
        case 100: ret = MSG_ButtonSetTransform(data); break;
        case 101: ret = MSG_ButtonSetText(data); break;
        case 102: ret = MSG_ButtonGetState(data); break;

        case 112: ret = MSG_PlayWAV(data); break;
        case 113: ret = MSG_IsWAVPlaying(data); break;
        case 114: ret = MSG_StopWAV(data); break;

        case ~0U: ret = MSG_Disconnect(data);        break;
        }

        return ret;
    }

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
        case 0:  t = PrimitiveType.Sphere;   break;
        case 1:  t = PrimitiveType.Capsule;  break;
        case 2:  t = PrimitiveType.Cylinder; break;
        case 3:  t = PrimitiveType.Cube;     break;
        case 4:  t = PrimitiveType.Plane;    break;
        default: t = PrimitiveType.Quad;     break;
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
        go.transform.localScale    = locscale;

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
        string str;
        try { str = System.Text.Encoding.UTF8.GetString(data); } catch { return 0; }
        m_tts.GetComponent<TextToSpeech>().StartSpeaking(str);
        return 1;
    }




    string GetFullPath(string file_name)
    {
        return Application.persistentDataPath + "/" + file_name;
    }

    string UnpackString(byte[] data, int index, int count)
    {
        try { return System.Text.Encoding.UTF8.GetString(data, index, count); } catch { return null; }
    }

    Quaternion conjugate(Quaternion q)
    {
        return new Quaternion(-q.x, -q.y, -q.z, q.w);
    }

    uint MSG_FileExists(byte[] data)
    {
        string file_name = UnpackString(data, 0, data.Length);
        if (file_name == null) { return 1; }

        string path = GetFullPath(file_name);
        return File.Exists(path) ? 0U : 2U;
    }

    uint MSG_FileUpload(byte[] data)
    {
        int name_offset = BitConverter.ToInt32(data, 0);
        int data_offset = BitConverter.ToInt32(data, 4);

        string file_name = UnpackString(data, name_offset, data_offset - name_offset);
        if (file_name == null) { return 1; }

        string path = GetFullPath(file_name);
        try { using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write)) { fs.Write(data, data_offset, data.Length - data_offset); } } catch { return 2; }
        
        return 0;
    }

    uint MSG_PanelCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_data - offset_name);
        if (name == null) { return 1; }

        if (m_panel_manifest.ContainsKey(name)) { return 2; }

        float dx = BitConverter.ToSingle(data, offset_data + 0);
        float dy = BitConverter.ToSingle(data, offset_data + 4);
        float dz = BitConverter.ToSingle(data, offset_data + 8);

        Vector3 position = (dx*transform.right + dy*transform.up + dz*transform.forward) + transform.position;

        GameObject panel = Instantiate(m_panel_sample, position, transform.rotation);
        if (panel == null) { return 3; }
        m_panel_manifest.Add(name, panel);

        panel.name = name;

        return 0;
    }

    uint MSG_PanelExists(byte[] data)
    {
        string name = UnpackString(data, 0, data.Length);
        if (name == null) { return 1; }
        return m_panel_manifest.ContainsKey(name) ? 0U : 2U;
    }

    uint MSG_PanelDestroy(byte[] data)
    {
        string name = UnpackString(data, 0, data.Length);
        if (name == null) { return 1; }

        if (!m_panel_manifest.ContainsKey(name)) { return 2; }
        GameObject panel = m_panel_manifest[name];
        m_panel_manifest.Remove(name);
        if (panel == null) { return 3; }

        Destroy(panel);

        return 0;
    }

    uint MSG_PanelSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_data - offset_name);
        if (name == null) { return 1; }

        if (!m_panel_manifest.ContainsKey(name)) { return 2; }
        GameObject panel = m_panel_manifest[name];
        if (panel == null) { return 3; }

        bool active = BitConverter.ToInt32(data, offset_data) != 0;
        panel.SetActive(active);

        return 0;
    }

    uint MSG_PanelSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_data = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_data - offset_name);
        if (name == null) { return 1; }

        if (!m_panel_manifest.ContainsKey(name)) { return 1; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 2; }

        float pin_tx = BitConverter.ToSingle(data, offset_data + 0);
        float pin_ty = BitConverter.ToSingle(data, offset_data + 4);
        float pin_tz = BitConverter.ToSingle(data, offset_data + 8);

        float pan_tx = BitConverter.ToSingle(data, offset_data + 12);
        float pan_ty = BitConverter.ToSingle(data, offset_data + 16);
        float pan_tz = BitConverter.ToSingle(data, offset_data + 20);

        float pan_sx = BitConverter.ToSingle(data, offset_data + 24);
        float pan_sy = BitConverter.ToSingle(data, offset_data + 28);
        float pan_sz = BitConverter.ToSingle(data, offset_data + 32);

        Transform backplate_transform = panel.transform.Find("Backplate/Quad");
        if (backplate_transform == null) { return 3; }

        backplate_transform.localPosition = new Vector3(pan_tx, pan_ty, pan_tz);
        backplate_transform.localScale = new Vector3(pan_sx, pan_sy, pan_sz);

        Transform pin_transform = panel.transform.Find("ButtonPin");
        if (pin_transform == null) { return 4; }

        pin_transform.localPosition = new Vector3(pin_tx, pin_ty, pin_tz);

        return 0;
    }

    uint MSG_SurfaceCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        if (panel.transform.Find(id) != null) { return 5; }

        GameObject child = Instantiate(m_surface_sample, panel.transform);
        child.name = id;

        return 0;
    }

    uint MSG_SurfaceExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        return (panel.transform.Find(id) != null) ? 0U : 5U;
    }

    uint MSG_SurfaceDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        Destroy(child_transform.gameObject);

        return 0;
    }
        
    uint MSG_SurfaceSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        bool active = BitConverter.ToInt32(data, offset_data) != 0;
        child_transform.gameObject.SetActive(active);

        return 0;
    }

    uint MSG_SurfaceSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        float px = BitConverter.ToSingle(data, offset_data + 0);
        float py = BitConverter.ToSingle(data, offset_data + 4);
        float pz = BitConverter.ToSingle(data, offset_data + 8);

        float qx = BitConverter.ToSingle(data, offset_data + 12);
        float qy = BitConverter.ToSingle(data, offset_data + 16);
        float qz = BitConverter.ToSingle(data, offset_data + 20);
        float qw = BitConverter.ToSingle(data, offset_data + 24);

        float sx = BitConverter.ToSingle(data, offset_data + 28);
        float sy = BitConverter.ToSingle(data, offset_data + 32);
        float sz = BitConverter.ToSingle(data, offset_data + 36);

        child_transform.localPosition = new Vector3(px, py, pz);
        child_transform.localRotation = new Quaternion(qx, qy, qz, qw);
        child_transform.localScale = new Vector3(sx, sy, sz);

        return 0;
    }

    uint MSG_SurfaceSetTexture(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);
        string file_name = UnpackString(data, offset_data, data.Length - offset_data);

        if (name == null) { return 1; }
        if (id == null) { return 2; }
        if (file_name == null) { return 3; }

        if (!m_panel_manifest.ContainsKey(name)) { return 4; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 5; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 6; }

        string path = GetFullPath(file_name);
        Texture2D tex;
        tex = new Texture2D(2, 2);
        byte[] image = File.ReadAllBytes(path);
        tex.LoadImage(image);

        child_transform.gameObject.GetComponent<Renderer>().material.mainTexture = tex;

        return 0;
    }

    uint MSG_SurfaceSetVideo(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_file = BitConverter.ToInt32(data, 8);
        int offset_data = BitConverter.ToInt32(data, 12);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_file - offset_id);
        string file_name = UnpackString(data, offset_file, offset_data - offset_file);

        if (name == null) { return 1; }
        if (id == null) { return 2; }
        if (file_name == null) { return 3; }

        if (!m_panel_manifest.ContainsKey(name)) { return 4; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 5; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 6; }

        var video_player = child_transform.gameObject.GetComponent<UnityEngine.Video.VideoPlayer>();
        if (video_player == null) { return 7; }

        bool loop = BitConverter.ToInt32(data, offset_data) != 0;
        string path = GetFullPath(file_name);

        video_player.url = path;
        video_player.isLooping = loop;
        video_player.Play();

        return 0;
    }

    uint MSG_TextCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        if (panel.transform.Find(id) != null) { return 5; }

        GameObject child = Instantiate(m_text_sample, panel.transform);
        child.name = id;

        return 0;
    }

    uint MSG_TextExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        return (panel.transform.Find(id) != null) ? 0U : 5U;
    }

    uint MSG_TextDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        Destroy(child_transform.gameObject);

        return 0;
    }

    uint MSG_TextSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        bool active = BitConverter.ToInt32(data, offset_data) != 0;
        child_transform.gameObject.SetActive(active);

        return 0;
    }

    uint MSG_TextSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        RectTransform rt = child_transform.gameObject.GetComponent<RectTransform>();
        if (rt == null) { return 6; }

        float px = BitConverter.ToSingle(data, offset_data + 0);
        float py = BitConverter.ToSingle(data, offset_data + 4);
        float pz = BitConverter.ToSingle(data, offset_data + 8);

        float w = BitConverter.ToSingle(data, offset_data + 12);
        float h = BitConverter.ToSingle(data, offset_data + 16);

        rt.localPosition = new Vector3(px, py, pz);
        rt.sizeDelta = new Vector2(w, h);

        return 0;
    }

    uint MSG_TextSetFormat(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        TextMeshPro tmp = child_transform.gameObject.GetComponent<TextMeshPro>();
        if (tmp == null) { return 6; }

        int font_style = BitConverter.ToInt32(data, offset_data + 0);
        float font_size = BitConverter.ToSingle(data, offset_data + 4);
        bool enable_auto_sizing = BitConverter.ToSingle(data, offset_data + 8) != 0;
        float color_r = BitConverter.ToSingle(data, offset_data + 12);
        float color_g = BitConverter.ToSingle(data, offset_data + 16);
        float color_b = BitConverter.ToSingle(data, offset_data + 20);
        float color_a = BitConverter.ToSingle(data, offset_data + 24);
        int h_alignment = BitConverter.ToInt32(data, offset_data + 28);
        int v_alignment = BitConverter.ToInt32(data, offset_data + 32);
        bool wrap = BitConverter.ToInt32(data, offset_data + 36) != 0;
        int overflow = BitConverter.ToInt32(data, offset_data + 40);

        tmp.fontStyle = (FontStyles)font_style;
        tmp.fontSize = font_size;
        tmp.enableAutoSizing = enable_auto_sizing;
        tmp.color = new Color(color_r, color_g, color_b, color_a);
        tmp.horizontalAlignment = (HorizontalAlignmentOptions)h_alignment;
        tmp.verticalAlignment = (VerticalAlignmentOptions)v_alignment;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = (TextOverflowModes)overflow;

        return 0;
    }

    uint MSG_TextSetText(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        TextMeshPro tmp = child_transform.gameObject.GetComponent<TextMeshPro>();
        if (tmp == null) { return 6; }

        string text = UnpackString(data, offset_data, data.Length - offset_data);
        if (text == null) { return 7; }

        tmp.text = text;

        return 0;
    }





    uint MSG_ButtonCreate(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        if (panel.transform.Find(id) != null) { return 5; }

        GameObject child = Instantiate(m_button_sample, panel.transform);
        child.name = id;

        ButtonEvent be = child.GetComponent<ButtonEvent>();
        if (be == null) { return 6; }

        be.pressed = false;
        be.index = BitConverter.ToInt32(data, offset_data + 0) & 31;

        return 0;
    }

    uint MSG_ButtonExists(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        return (panel.transform.Find(id) != null) ? 0U : 5U;
    }

    uint MSG_ButtonDestroy(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, data.Length - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        Destroy(child_transform.gameObject);

        return 0;
    }

    uint MSG_ButtonSetActive(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        bool active = BitConverter.ToInt32(data, offset_data) != 0;
        child_transform.gameObject.SetActive(active);

        return 0;
    }

    uint MSG_ButtonSetTransform(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        float px = BitConverter.ToSingle(data, offset_data + 0);
        float py = BitConverter.ToSingle(data, offset_data + 4);
        float pz = BitConverter.ToSingle(data, offset_data + 8);

        float qx = BitConverter.ToSingle(data, offset_data + 12);
        float qy = BitConverter.ToSingle(data, offset_data + 16);
        float qz = BitConverter.ToSingle(data, offset_data + 20);
        float qw = BitConverter.ToSingle(data, offset_data + 24);

        float sx = BitConverter.ToSingle(data, offset_data + 28);
        float sy = BitConverter.ToSingle(data, offset_data + 32);
        float sz = BitConverter.ToSingle(data, offset_data + 36);

        child_transform.localPosition = new Vector3(px, py, pz);
        child_transform.localRotation = new Quaternion(qx, qy, qz, qw);
        child_transform.localScale = new Vector3(sx, sy, sz);

        return 0;
    }

    uint MSG_ButtonSetText(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_id = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_id - offset_name);
        string id = UnpackString(data, offset_id, offset_data - offset_id);

        if (name == null) { return 1; }
        if (id == null) { return 2; }

        if (!m_panel_manifest.ContainsKey(name)) { return 3; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 4; }

        Transform child_transform = panel.transform.Find(id);
        if (child_transform == null) { return 5; }

        Transform icon_and_text = child_transform.Find("IconAndText");
        if (icon_and_text == null) { return 6; }

        Transform text_mesh_pro = icon_and_text.Find("TextMeshPro");
        if (text_mesh_pro == null) { return 7; }

        TextMeshPro tmp = text_mesh_pro.gameObject.GetComponent<TextMeshPro>();
        if (tmp == null) { return 8; }

        string text = UnpackString(data, offset_data, data.Length - offset_data);
        if (text == null) { return 9; }

        tmp.text = text;

        return 0;
    }

    uint MSG_ButtonGetState(byte[] data)
    {
        string name = UnpackString(data, 0, data.Length);
        if (name == null) { return 0x80000001; }

        if (!m_panel_manifest.ContainsKey(name)) { return 0x80000002; }
        GameObject panel = m_panel_manifest[name];
        if (!panel) { return 0x80000003; }

        uint ret = 0;

        ButtonEvent[] list = panel.GetComponentsInChildren<ButtonEvent>();
        foreach (var e in list)
        {
            ret |= (e.pressed ? 1U : 0U) << e.index;
            e.pressed = false;
        }

        return ret;
    }

    uint MSG_PlayWAV(byte[] data)
    {
        int offset_name = BitConverter.ToInt32(data, 0);
        int offset_format = BitConverter.ToInt32(data, 4);
        int offset_data = BitConverter.ToInt32(data, 8);

        string name = UnpackString(data, offset_name, offset_format - offset_name);
        if (name == null) { return 1; }

        int channels = BitConverter.ToInt32(data, offset_format + 0);
        int frequency = BitConverter.ToInt32(data, offset_format + 4);
        bool clear = BitConverter.ToInt32(data, offset_format + 8) != 0;

        int data_size = data.Length - offset_data;
        int lengthSamples = data_size / (channels * sizeof(float));

        AudioClip audio_clip = AudioClip.Create(name, lengthSamples, channels, frequency, false);
        if (!audio_clip) { return 2; }

        float[] samples = new float[data_size / sizeof(float)];
        Buffer.BlockCopy(data, offset_data, samples, 0, data_size);
        if (!audio_clip.SetData(samples, 0)) { return 3; }

        var source = m_audio.GetComponent<AudioSource>();
        if (!source) { return 4; }

        if (clear) { source.Stop(); }
        source.PlayOneShot(audio_clip);        
        
        return 0;
    }

    uint MSG_IsWAVPlaying(byte[] data)
    {
        var source = m_audio.GetComponent<AudioSource>();
        if (!source) { return 2; }
        return source.isPlaying ? 1U : 0U;
    }

    uint MSG_StopWAV(byte[] data)
    {
        var source = m_audio.GetComponent<AudioSource>();
        if (!source) { return 1; }

        source.Stop();
        return 0;
    }




}
