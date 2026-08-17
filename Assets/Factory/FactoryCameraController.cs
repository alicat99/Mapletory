using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class FactoryCameraController : MonoBehaviour
    {
        [SerializeField] private float movement_speed = 8f;
        [SerializeField] private float zoom_speed = 0.15f;
        [SerializeField] private float minimum_zoom = 3f;
        [SerializeField] private float maximum_zoom = 14f;

        private Camera controlled_camera;
        private ConveyorBuilder conveyor_builder;
        private Bounds map_bounds;
        private bool is_panning;

        public void Initialize(Renderer ground_renderer, ConveyorBuilder builder)
        {
            controlled_camera = GetComponent<Camera>();
            conveyor_builder = builder;
            map_bounds = ground_renderer.bounds;
        }

        private void Update()
        {
            if (Keyboard.current != null)
            {
                MoveCamera();
            }

            if (Mouse.current != null)
            {
                ZoomCamera();
                PanCamera();
            }

            ClampPosition();
        }

        private void MoveCamera()
        {
            var horizontal = ReadAxis(
                Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed,
                Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed);
            var vertical = ReadAxis(
                Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed,
                Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed);
            var movement = new Vector3(horizontal, vertical) * (movement_speed * Time.unscaledDeltaTime);
            transform.position += movement;
        }

        private void ZoomCamera()
        {
            var scroll = Mouse.current.scroll.ReadValue().y;
            controlled_camera.orthographicSize = Mathf.Clamp(
                controlled_camera.orthographicSize - scroll * zoom_speed,
                minimum_zoom,
                maximum_zoom);
        }

        private void PanCamera()
        {
            if (conveyor_builder.IsBuildMode)
            {
                is_panning = false;
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                is_panning = EventSystem.current == null
                    || !EventSystem.current.IsPointerOverGameObject();
            }

            if (!Mouse.current.leftButton.isPressed)
            {
                is_panning = false;
                return;
            }

            if (!is_panning)
            {
                return;
            }

            var screen_delta = Mouse.current.delta.ReadValue();
            var world_units_per_pixel = controlled_camera.orthographicSize * 2f / Screen.height;
            transform.position -= new Vector3(screen_delta.x, screen_delta.y) * world_units_per_pixel;
        }

        private void ClampPosition()
        {
            var camera_height = controlled_camera.orthographicSize;
            var camera_width = camera_height * controlled_camera.aspect;
            var position = transform.position;

            var min_x = map_bounds.min.x + Mathf.Min(camera_width, map_bounds.extents.x);
            var max_x = map_bounds.max.x - Mathf.Min(camera_width, map_bounds.extents.x);
            var min_y = map_bounds.min.y + Mathf.Min(camera_height, map_bounds.extents.y);
            var max_y = map_bounds.max.y - Mathf.Min(camera_height, map_bounds.extents.y);
            position.x = Mathf.Clamp(position.x, min_x, max_x);
            position.y = Mathf.Clamp(position.y, min_y, max_y);
            transform.position = position;
        }

        private static float ReadAxis(bool negative, bool positive)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
