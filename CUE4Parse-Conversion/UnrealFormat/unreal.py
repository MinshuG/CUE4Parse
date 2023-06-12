import bpy
import struct
import os
import io
import time
import gzip
import zstd
from mathutils import Vector, Matrix, Quaternion, Euler
from math import *

class Log:
	COLOR = u"\u001b[36m"
	RESET = u"\u001b[0m"
	
	def write(text):
		print(f"[{Log.COLOR}UNREAL{Log.RESET}] {text}")


def bytes_to_str(in_bytes):
	return in_bytes.rstrip(b'\x00').decode()

class FArchiveReader:
	data = None
	size = 0

	def __init__(self, data):
		self.data = io.BytesIO(data)
		self.size = len(self.data.read())
		self.data.seek(0)

	def __enter__(self):
		self.size = len(self.data.read())
		self.data.seek(0)
		return self

	def __exit__(self, type, value, traceback):
		self.data.close()

	def eof(self):
		return self.data.tell() >= self.size

	def read(self, size: int):
		return self.data.read(size)

	def read_to_end(self):
		return self.data.read(self.size - self.data.tell())

	def read_bool(self):
		return struct.unpack("?", self.data.read(1))[0]

	def read_string(self, size: int):
		string =  self.data.read(size)
		return bytes_to_str(string)

	def read_fstring(self):
		size, = struct.unpack("i", self.data.read(4))
		string = self.data.read(size)
		return bytes_to_str(string)

	def read_int(self):
		return struct.unpack("i", self.data.read(4))[0]

	def read_int_vector(self, size: int):
		return struct.unpack(str(size) + "I", self.data.read(size*4))

	def read_short(self):
		return struct.unpack("h", self.data.read(2))[0]

	def read_byte(self):
		return struct.unpack("c", self.data.read(1))[0]

	def read_float(self):
		return struct.unpack("f", self.data.read(4))[0]

	def read_float_vector(self, size: int):
		return struct.unpack(str(size) + "f", self.data.read(size*4))

	def read_byte(self):
		return struct.unpack("c", self.data.read(1))

	def read_byte_vector(self, size: int):
		return struct.unpack(str(size) + "B", self.data.read(size))

	def skip(self, size: int):
		self.data.read(size)

	def read_bulk_array(self, count: int, predicate):
		bulk_array = []
		for counter in range(count):
			bulk_array.append(predicate(self))
		return bulk_array

class UModel:
	vertices = []
	indices = []
	normals = []
	tangents = []
	colors = []
	uvs = []
	materials = []
	morphs = []
	weights = []
	bones = []
	sockets = []

class Bone:
	name = ""
	parent_index = -1
	position = []
	rotation = []

	def __init__(self, ar: FArchiveReader):
		self.name = ar.read_fstring()
		self.parent_index = ar.read_int()
		self.position = [pos * 0.01 for pos in ar.read_float_vector(3)]
		self.rotation = ar.read_float_vector(4)

class Weight:
	bone_index = -1
	vertex_index = -1
	weight = -1

	def __init__(self, ar: FArchiveReader):
		self.bone_index = ar.read_short()
		self.vertex_index = ar.read_int()
		self.weight = ar.read_float()

class MorphTarget:
	name = ""
	deltas = []

	def __init__(self, ar: FArchiveReader):
		self.name = ar.read_fstring()

		count = ar.read_int()
		self.deltas = ar.read_bulk_array(count, lambda archive: MorphTargetData(archive))

class MorphTargetData:
	position = []
	normals = []
	vertex_index = -1

	def __init__(self, ar: FArchiveReader):
		self.position = [pos * 0.01 for pos in ar.read_float_vector(3)]
		self.normals = ar.read_float_vector(3)
		self.vertex_index = ar.read_int()

class Socket:
	name = ""
	parent_name = ""
	position = []
	rotation = []
	scale = []

	def __init__(self, ar: FArchiveReader):
		self.name = ar.read_fstring()
		self.parent_name = ar.read_fstring()
		self.position = [pos * 0.01 for pos in ar.read_float_vector(3)]
		self.rotation = ar.read_float_vector(3)
		self.scale = ar.read_float_vector(3)

class UWorld:
	meshes = []
	actors = []

class HashedMesh:
	hash_number = 0
	data = []

	def __init__(self, ar: FArchiveReader):
		self.hash_number = ar.read_int()

		data_size = ar.read_int()
		self.data = ar.read(data_size)

class Actor:
	hash_number = 0
	name = ""
	position = []
	rotation = []
	scale = []

	def __init__(self, ar: FArchiveReader):
		self.hash_number = ar.read_int()
		self.name = ar.read_fstring()
		self.position = [pos * 0.01 for pos in ar.read_float_vector(3)]
		self.rotation = ar.read_float_vector(3)
		self.scale = ar.read_float_vector(3)

MAGIC = "UNREALFORMAT"
MESH_INDENTIFIER = "UMODEL"
WORLD_INDENTIFIER = "UWORLD"
ANIM_INDENTIFIER = "UANIM"

def main():
	start = time.perf_counter()
	import_file("D:/Exports/Fortnite/Game/Maps/UI/Frontend_Lobby_StW_Season10.uworld")
	end = time.perf_counter()
	Log.write(f"Took {str(round(end - start, 3))}s")

def import_file(path: str):
	with open(path, 'rb') as file:
		import_data(file.read())

def import_data(data, link_model: bool = True):
	with FArchiveReader(data) as ar:
		magic = ar.read_string(len(MAGIC))
		if magic != MAGIC:
			return

		identifier = ar.read_fstring()
		file_version = ar.read_int()
		object_name = ar.read_fstring()
		Log.write(f"Importing {object_name}")

		read_archive = ar
		is_compressed = ar.read_bool()
		if is_compressed:
			compression_type = ar.read_fstring()
			Log.write(f"Compressed with {compression_type}")

			if compression_type == "GZIP":
				read_archive = FArchiveReader(gzip.decompress(ar.read_to_end()))
			elif compression_type == "ZSTD":
				read_archive = FArchiveReader(zstd.ZSTD_uncompress(ar.read_to_end()))
			else:
				Log.write(f"Unknown Compression Type: {compression_type}")
				return


		if identifier == MESH_INDENTIFIER:
			return import_umodel_data(read_archive, object_name, link_model)
		elif identifier == WORLD_INDENTIFIER:
			return import_uworld_data(read_archive, object_name)
	

def import_uworld_data(ar: FArchiveReader, name: str):
	uworld = UWorld()

	while not ar.eof():
		header_name = ar.read_fstring()
		array_size = ar.read_int()
		byte_size = ar.read_int()

		if header_name == "MESHES":
			uworld.meshes = ar.read_bulk_array(array_size, lambda ar: HashedMesh(ar))
		elif header_name == "ACTORS":
			uworld.actors = ar.read_bulk_array(array_size, lambda ar: Actor(ar))
		else:
			ar.skip(byte_size)

	mesh_map = {}
	for mesh in uworld.meshes:
		mesh_map[mesh.hash_number] = import_data(mesh.data, False)

	for actor in uworld.actors:
		target_mesh = mesh_map[actor.hash_number]
		obj = bpy.data.objects.new(actor.name, target_mesh.data)
		obj.location = actor.position
		obj.rotation_mode = 'XYZ'
		obj.rotation_euler = [radians(actor.rotation[2]), radians(actor.rotation[0]), radians(actor.rotation[1])]
		obj.scale = actor.scale
		bpy.context.scene.collection.objects.link(obj)

def import_umodel_data(ar: FArchiveReader, name: str, link: bool):
	umodel = UModel()

	while not ar.eof():
		header_name = ar.read_fstring()
		array_size = ar.read_int()
		byte_size = ar.read_int()

		if header_name == "VERTICES":
			umodel.vertices = ar.read_bulk_array(array_size, lambda ar: [vert * 0.01 for vert in ar.read_float_vector(3)])
		elif header_name == "INDICES":
			umodel.indices = ar.read_bulk_array(int(array_size / 3), lambda ar: ar.read_int_vector(3))
		elif header_name == "NORMALS":
			umodel.normals = ar.read_bulk_array(array_size, lambda ar: ar.read_float_vector(3))
		elif header_name == "TANGENTS":
			umodel.tangents = ar.read_bulk_array(array_size, lambda ar: ar.read_float_vector(3))
		elif header_name == "VERTEXCOLORS":
			umodel.colors = ar.read_bulk_array(array_size, lambda ar: ar.read_byte_vector(4))
		elif header_name == "TEXCOORDS":
			umodel.uvs = ar.read_bulk_array(array_size, lambda ar: ar.read_float_vector(2))
		elif header_name == "MATERIALS":
			umodel.materials = ar.read_bulk_array(array_size, lambda ar: ar.read_fstring())
		elif header_name == "WEIGHTS":
			umodel.weights = ar.read_bulk_array(array_size, lambda ar: Weight(ar))
		elif header_name == "BONES":
			umodel.bones = ar.read_bulk_array(array_size, lambda ar: Bone(ar))
		elif header_name == "MORPHTARGETS":
			umodel.morphs = ar.read_bulk_array(array_size, lambda ar: MorphTarget(ar))
		elif header_name == "SOCKETS":
			umodel.sockets = ar.read_bulk_array(array_size, lambda ar: Socket(ar))
		else:
			ar.skip(byte_size)

	# geometry
	mesh_data = bpy.data.meshes.new(name)
	mesh_data.from_pydata(umodel.vertices, [], umodel.indices)

	mesh_object = bpy.data.objects.new(name, mesh_data)
	return_object = mesh_object
	if link:
		bpy.context.scene.collection.objects.link(mesh_object)

	# normals
	if len(umodel.normals) > 0:
		mesh_data.polygons.foreach_set("use_smooth", [True] * len(mesh_data.polygons))
		mesh_data.normals_split_custom_set_from_vertices(umodel.normals)
		mesh_data.use_auto_smooth = True

	# weights
	if len(umodel.weights) > 0 and len(umodel.bones) > 0:
		for weight in umodel.weights:
			bone_name = umodel.bones[weight.bone_index].name
			vertex_group = mesh_object.vertex_groups.get(bone_name)
			if not vertex_group:
				vertex_group = mesh_object.vertex_groups.new(name = bone_name)
			vertex_group.add([weight.vertex_index], weight.weight, 'ADD')

	# morph targets
	if len(umodel.morphs) > 0:
		default_key = mesh_object.shape_key_add(from_mix=False)
		default_key.name = "Default"
		default_key.interpolation = 'KEY_LINEAR'

		for morph in umodel.morphs:
			key = mesh_object.shape_key_add(from_mix=False)
			key.name = morph.name
			key.interpolation = 'KEY_LINEAR'

			for delta in morph.deltas:
				key.data[delta.vertex_index].co += Vector(delta.position)
	# vertex colors
	if len(umodel.colors) > 0:
		vertex_color = mesh_data.vertex_colors.new(name="COL0", do_init=False)
		for polygon in mesh_data.polygons:
			for vertex_index, loop_index in zip(polygon.vertices, polygon.loop_indices):
				color = umodel.colors[vertex_index]

	# texture coordinates
	if len(umodel.uvs) > 0:
		uv_base = mesh_data.uv_layers.new(name="UV0")
		for polygon in mesh_data.polygons:
			for vertex_index, loop_index in zip(polygon.vertices, polygon.loop_indices):
				uv_base.data[loop_index].uv = umodel.uvs[vertex_index]

	# materials
	if len(umodel.materials) > 0:
		for material_name in umodel.materials:
			if material_name == "":
				mesh_data.materials.append(None)
				continue

			material = bpy.data.materials.get(material_name)
			if material is None:
				material = bpy.data.materials.new(name=material_name)
			mesh_data.materials.append(material)

	# skeleton
	if len(umodel.bones) > 0 or len(umodel.sockets) > 0:
		mesh_object.name += "_MESH"

		armature_data = bpy.data.armatures.new(name=name)
		armature_data.display_type = 'STICK'

		armature_object = bpy.data.objects.new(name, armature_data)
		armature_object.show_in_front = True
		return_object = armature_object

		if link:
			bpy.context.scene.collection.objects.link(armature_object)
		bpy.context.view_layer.objects.active = armature_object
		armature_object.select_set(True)

		mesh_object.parent = armature_object

	if len(umodel.bones) > 0:
	 	# create bones
		bpy.ops.object.mode_set(mode='EDIT')
		edit_bones = armature_data.edit_bones
		for bone in umodel.bones:
			bone_pos = Vector(bone.position)
			bone_rot = Quaternion((bone.rotation[3], bone.rotation[0], bone.rotation[1], bone.rotation[2])) # xyzw -> wxyz

			edit_bone = edit_bones.new(bone.name)
			edit_bone.length = 5.0 * 0.01

			bone_matrix = Matrix.Translation(bone_pos) @ bone_rot.to_matrix().to_4x4()

			if bone.parent_index >= 0:
				parent_bone = edit_bones.get(umodel.bones[bone.parent_index].name)
				edit_bone.parent = parent_bone
				bone_matrix = parent_bone.matrix @ bone_matrix

			edit_bone.matrix = bone_matrix
		bpy.ops.object.mode_set(mode='OBJECT')

		# armature modifier
		armature_modifier = mesh_object.modifiers.new(armature_object.name, type='ARMATURE')
		armature_modifier.show_expanded = False
		armature_modifier.use_vertex_groups = True
		armature_modifier.object = armature_object

		# bone colors
		bpy.ops.object.mode_set(mode='POSE')
		leaf_group = armature_object.pose.bone_groups.new(name='Leaf Bones')
		leaf_group.color_set = 'THEME03'

		unused_group = armature_object.pose.bone_groups.new(name='Weightless Bones')
		unused_group.color_set = 'THEME10'

		for bone in armature_object.pose.bones:
			if len(bone.children) == 0:
				bone.bone_group = leaf_group

			if mesh_object.vertex_groups.get(bone.name) is None:
				bone.bone_group = unused_group

		bpy.ops.object.mode_set(mode='OBJECT')

	# sockets
	if len(umodel.sockets) > 0:
		# create sockets
		bpy.ops.object.mode_set(mode='EDIT')
		for socket in umodel.sockets:
			socket_bone = edit_bones.new(socket.name)
			parent_bone = edit_bones.get(socket.parent_name)
			socket_bone.parent = parent_bone
			socket_bone.length = 5.0 * 0.01
			socket_bone.matrix = parent_bone.matrix @ Matrix.LocRotScale(Vector(socket.position), Euler(socket.rotation), Vector(socket.scale))
		bpy.ops.object.mode_set(mode='POSE')

		# socket colors
		socket_group = armature_object.pose.bone_groups.new(name='Sockets')
		socket_group.color_set = 'THEME05'
		for socket in umodel.sockets:
			armature_object.pose.bones.get(socket.name).bone_group = socket_group
		bpy.ops.object.mode_set(mode='OBJECT')

	return return_object

if __name__ == "__main__":
	main()