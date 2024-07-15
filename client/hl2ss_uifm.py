
import struct
import hl2ss


# 3D Primitive Types
class PrimitiveType:
    Sphere = 0
    Capsule = 1
    Cylinder = 2
    Cube = 3
    Plane = 4
    Quad = 5


# Server Target Mode
class TargetMode:
    UseID = 0
    UseLast = 1


# Object Active State
class ActiveState:
    Inactive = 0
    Active = 1


class FontStyles:
    Normal = 0
    Bold = 1
    Italic = 2
    Underline = 4
    LowerCase = 8
    UpperCase = 16
    SmallCaps = 32
    Strikethrough = 64
    Superscript = 128
    Subscript = 256
    Highlight = 512


class HorizontalAlignmentOptions:
    Left = 1
    Center = 2
    Right = 4
    Justified = 8
    Flush = 16
    Geometry = 32


class VerticalAlignmentOptions:
    Top = 256
    Middle = 512
    Bottom = 1024
    Baseline = 2048
    Geometry = 4096
    Capline = 8192


class TextOverflowModes:
    Overflow = 0
    Ellipsis = 1
    Masking = 2
    Truncate = 3
    ScrollRect = 4
    Page = 5
    Linked = 6


#------------------------------------------------------------------------------
# Commands
#------------------------------------------------------------------------------

class command_buffer(hl2ss.umq_command_buffer):
    def create_primitive(self, type):
        self.add(0, struct.pack('<I', type))

    def set_active(self, key, state):
        self.add(1, struct.pack('<II', key, state))

    def set_world_transform(self, key, position, rotation, scale):
        self.add(2, struct.pack('<Iffffffffff', key, position[0], position[1], position[2], rotation[0], rotation[1], rotation[2], rotation[3], scale[0], scale[1], scale[2]))

    def set_local_transform(self, key, position, rotation, scale):
        self.add(3, struct.pack('<Iffffffffff', key, position[0], position[1], position[2], rotation[0], rotation[1], rotation[2], rotation[3], scale[0], scale[1], scale[2]))

    def set_color(self, key, rgba):
        self.add(4, struct.pack('<Iffff', key, rgba[0], rgba[1], rgba[2], rgba[3]))

    def set_texture(self, key, texture):
        self.add(5, struct.pack('<I', key) + texture)

    def create_text(self): 
        self.add(6, b'')

    def set_text(self, key, font_size, rgba, string):
        self.add(7, struct.pack('<Ifffff', key, font_size, rgba[0], rgba[1], rgba[2], rgba[3]) + string.encode('utf-8'))

    def say(self, text):
        self.add(8, text.encode('utf-8'))

    def load_mesh(self, data):
        self.add(15, data)

    def remove(self, key):
        self.add(16, struct.pack('<I', key))

    def remove_all(self):
        self.add(17, b'')

    def begin_display_list(self):
        self.add(18, b'')

    def end_display_list(self):
        self.add(19, b'')

    def set_target_mode(self, mode):
        self.add(20, struct.pack('<I', mode))

    # File IO Map -------------------------------------------------------------

    def file_exists(self, filename):
        text = filename.encode('utf-8')
        self.add(32, text)

    def file_upload(self, filename, data):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = filename.encode('utf-8')
        r1 = data
        self.add(33, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    # UI Map ------------------------------------------------------------------

    def panel_create(self, name, local_position):
        f = '<II'
        rb = struct.calcsize(f) 
        r0 = name.encode('utf-8')
        r1 = struct.pack('<fff', local_position[0], local_position[1], local_position[2])
        self.add(48, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def panel_exists(self, name):
        r0 = name.encode('utf-8')
        self.add(49, r0)

    def panel_destroy(self, name):
        r0 = name.encode('utf-8')
        self.add(50, r0)

    def panel_set_active(self, name, active):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = name.encode('utf-8')
        r1 = struct.pack('<I', 1 if (active) else 0)
        self.add(51, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def panel_set_transform(self, name, pin_position, panel_positon, panel_scale):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = name.encode('utf-8')
        r1 = struct.pack('<fffffffff', pin_position[0], pin_position[1], pin_position[2], panel_positon[0], panel_positon[1], panel_positon[2], panel_scale[0], panel_scale[1], panel_scale[2])
        self.add(52, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def surface_create(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(64, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def surface_exists(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(65, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def surface_destroy(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(66, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def surface_set_active(self, parent, name, active):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<I', 1 if (active) else 0)
        self.add(67, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def surface_set_transform(self, parent, name, position, rotation, scale):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<ffffffffff', position[0], position[1], position[2], rotation[0], rotation[1], rotation[2], rotation[3], scale[0], scale[1], scale[2])
        self.add(68, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def surface_set_texture(self, parent, name, file_name):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = file_name.encode('utf-8')
        self.add(69, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def surface_set_video(self, parent, name, file_name, loop):
        f = "<IIII"
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = file_name.encode('utf-8')
        r3 = struct.pack('<I', 1 if (loop) else 0)
        self.add(70, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1), rb + len(r0) + len(r1) + len(r2)) + r0 + r1 + r2 + r3)

    def text_create(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(80, struct.pack(f, rb, rb + len(r0)) + r0 + r1)
        
    def text_exists(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(81, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def text_destroy(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(82, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def text_set_active(self, parent, name, active):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<I', 1 if (active) else 0)
        self.add(83, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def text_set_transform(self, parent, name, position, dimensions):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<fffff', position[0], position[1], position[2], dimensions[0], dimensions[1])
        self.add(84, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def text_set_format(self, parent, name, font_style, font_size, enable_auto_sizing, rgba_color, horizontal_alignment, vertical_alignment, enable_word_wrap, overflow_mode):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<IfIffffIIII', font_style, font_size, 1 if (enable_auto_sizing) else 0, rgba_color[0], rgba_color[1], rgba_color[2], rgba_color[3], horizontal_alignment, vertical_alignment, 1 if (enable_word_wrap) else 0, overflow_mode)
        self.add(85, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def text_set_text(self, parent, name, text):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = text.encode('utf-8')
        self.add(86, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def button_create(self, parent, name, index):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<I', index)
        self.add(96, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def button_exists(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(97, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def button_destroy(self, parent, name):
        f = '<II'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        self.add(98, struct.pack(f, rb, rb + len(r0)) + r0 + r1)

    def button_set_active(self, parent, name, active):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<I', 1 if (active) else 0)
        self.add(99, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def button_set_transform(self, parent, name, position, rotation, scale):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = struct.pack('<ffffffffff', position[0], position[1], position[2], rotation[0], rotation[1], rotation[2], rotation[3], scale[0], scale[1], scale[2])
        self.add(100, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def button_set_text(self, parent, name, text):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = parent.encode('utf-8')
        r1 = name.encode('utf-8')
        r2 = text.encode('utf-8')
        self.add(101, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def button_get_state(self, parent):
        r0 = parent.encode('utf-8')
        self.add(102, r0)

    def play_wav(self, name, clear, channels, sample_rate, data):
        f = '<III'
        rb = struct.calcsize(f)
        r0 = name.encode('utf-8')
        r1 = struct.pack('<III', channels, sample_rate, 1 if (clear) else 0)
        r2 = data
        self.add(112, struct.pack(f, rb, rb + len(r0), rb + len(r0) + len(r1)) + r0 + r1 + r2)

    def is_wav_playing(self):
        self.add(113, b'')

    def stop_wav(self):
        self.add(114, b'')

        