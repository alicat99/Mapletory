using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Maptory.Factory
{
    public sealed class FactoryCameraController : MonoBehaviour
    {
        public event Action<float> Panned;
        public event Action<float> Zoomed;
        [SerializeField] private float movement_speed = 8f;
        [SerializeField] private float zoom_speed = 0.3f;
        [SerializeField] private float minimum_zoom = 3f;
        [SerializeField] private float maximum_zoom = 14f;

        private Camera controlled_camera;
        private Renderer ground_renderer;
        private Func<bool> is_input_blocked;
        private bool is_panning;
        private bool is_pan_blocked;
        private Vector2 last_pointer_position;

        public void Initialize(Renderer ground_renderer, Func<bool> input_blocker = null)
        {
            controlled_camera = GetComponent<Camera>();
            this.ground_renderer = ground_renderer;
            is_input_blocked = input_blocker;
        }

        private void Update()
        {
            if (is_input_blocked != null && is_input_blocked())
            {
                is_panning = false;
                is_pan_blocked = false;
                return;
            }

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
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var scroll = Mouse.current.scroll.ReadValue().y;
            var previous_zoom = controlled_camera.orthographicSize;
            controlled_camera.orthographicSize = Mathf.Clamp(
                controlled_camera.orthographicSize - scroll * zoom_speed,
                minimum_zoom,
                maximum_zoom);
            var zoom_delta = Mathf.Abs(controlled_camera.orthographicSize - previous_zoom);
            if (zoom_delta > 0f) Zoomed?.Invoke(zoom_delta);
        }

        private void PanCamera()
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                is_panning = false;
                is_pan_blocked = false;
                return;
            }

            if (!is_panning)
            {
                if (is_pan_blocked)
                {
                    return;
                }

                is_pan_blocked = EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject();
                last_pointer_position = Mouse.current.position.ReadValue();
                is_panning = !is_pan_blocked;
                return;
            }

            var pointer_position = Mouse.current.position.ReadValue();
            var screen_delta = pointer_position - last_pointer_position;
            last_pointer_position = pointer_position;
            var world_units_per_pixel = controlled_camera.orthographicSize * 2f / Screen.height;
            var movement = new Vector3(screen_delta.x, screen_delta.y) * world_units_per_pixel;
            transform.position -= movement;
            if (movement.sqrMagnitude > 0f) Panned?.Invoke(movement.magnitude);
        }

        private void ClampPosition()
        {
            var camera_height = controlled_camera.orthographicSize;
            var camera_width = camera_height * controlled_camera.aspect;
            var map_bounds = ground_renderer.bounds;
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
