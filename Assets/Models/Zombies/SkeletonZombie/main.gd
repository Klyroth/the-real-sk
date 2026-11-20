extends Node3D

@export var model_file: String = "res://models/skeleton-zombie.glb"
var actor: Node3D
var player: AnimationPlayer
var camera: Camera3D
var clip_picker: OptionButton
var pause_button: Button
var clips: Array = []

func _ready() -> void:
	var metadata = JSON.parse_string(FileAccess.get_file_as_string("res://animations.json"))
	clips = metadata.clips
	actor = load(model_file).instantiate()
	add_child(actor)
	player = actor.find_children("*", "AnimationPlayer", true, false)[0]
	for clip in clips:
		player.get_animation(clip.name).loop_mode = Animation.LOOP_LINEAR if clip.loop else Animation.LOOP_NONE
	var floor_mesh = MeshInstance3D.new()
	var plane = PlaneMesh.new()
	plane.size = Vector2(8,8)
	floor_mesh.mesh = plane
	var floor_material = StandardMaterial3D.new()
	floor_material.albedo_color = Color(0.18,0.19,0.21)
	floor_material.roughness = 0.9
	floor_mesh.material_override = floor_material
	add_child(floor_mesh)
	var environment = WorldEnvironment.new()
	environment.environment = Environment.new()
	environment.environment.background_mode = Environment.BG_COLOR
	environment.environment.background_color = Color(0.08,0.09,0.11)
	environment.environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	environment.environment.ambient_light_color = Color(0.85,0.89,1)
	environment.environment.ambient_light_energy = 0.65
	add_child(environment)
	var light = DirectionalLight3D.new()
	light.rotation_degrees = Vector3(-45,-30,0)
	light.light_energy = 1.4
	light.shadow_enabled = true
	add_child(light)
	camera = Camera3D.new()
	add_child(camera)
	camera.current = true
	camera.projection = Camera3D.PROJECTION_ORTHOGONAL
	camera.size = 2.7
	set_view(false)
	var canvas = CanvasLayer.new()
	add_child(canvas)
	var panel = PanelContainer.new()
	panel.position = Vector2(18,18)
	canvas.add_child(panel)
	var row = HBoxContainer.new()
	row.add_theme_constant_override("separation",12)
	panel.add_child(row)
	clip_picker = OptionButton.new()
	for clip in clips: clip_picker.add_item(clip.name.replace("Zombie", "Zombie "))
	clip_picker.item_selected.connect(func(i): play_clip(clips[i].name))
	row.add_child(clip_picker)
	var replay = Button.new()
	replay.text = "Replay"
	replay.pressed.connect(func(): play_clip(clips[clip_picker.selected].name))
	row.add_child(replay)
	pause_button = Button.new()
	pause_button.text = "Pause"
	pause_button.pressed.connect(toggle_pause)
	row.add_child(pause_button)
	for label in ["Front", "Side"]:
		var button = Button.new()
		button.text = label
		button.pressed.connect(func(): set_view(label == "Side"))
		row.add_child(button)
	play_clip(clips[0].name)

func play_clip(clip_name: String) -> void:
	for index in clips.size():
		if clips[index].name == clip_name: clip_picker.select(index)
	player.stop()
	player.play(clip_name)
	player.seek(0,true)
	pause_button.text = "Pause"

func toggle_pause() -> void:
	if player.is_playing():
		player.pause()
		pause_button.text = "Play"
	else:
		player.play()
		pause_button.text = "Pause"

func set_view(side: bool) -> void:
	camera.position = Vector3(4,1.25,0.15) if side else Vector3(0,1.25,4)
	camera.look_at(Vector3(0,0.88,0.12),Vector3.UP)
