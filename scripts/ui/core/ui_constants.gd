class_name UIConstants
extends RefCounted

## UIConstants
## Static constants for UI sizing, spacing, and mobile-friendly values
## All sizes are in logical pixels (dp/pt equivalent)

# Reference design dimensions
# Designed for 360dp width (Android minimum) and 375pt width (iOS minimum)
const REFERENCE_WIDTH: float = 1280.0
const REFERENCE_HEIGHT: float = 720.0

# Minimum touch target sizes (per platform guidelines)
# Android: 48dp minimum, iOS: 44pt minimum
# Doubled for 1280x720 reference resolution
const MIN_TOUCH_TARGET: float = 96.0
const MIN_TOUCH_TARGET_SMALL: float = 88.0  # For less critical elements

# Icon sizes (doubled for 1280x720 reference)
const ICON_SIZE_SMALL: int = 32
const ICON_SIZE_MEDIUM: int = 48
const ICON_SIZE_LARGE: int = 64
const ICON_SIZE_XLARGE: int = 96

# Button sizes (doubled for 1280x720 reference)
const BUTTON_HEIGHT_SMALL: float = 72.0
const BUTTON_HEIGHT_MEDIUM: float = 88.0
const BUTTON_HEIGHT_LARGE: float = 112.0
const BUTTON_MIN_WIDTH: float = 128.0

# Slot sizes for inventory/equipment (doubled for 1280x720 reference)
const SLOT_SIZE_SMALL: int = 96
const SLOT_SIZE_MEDIUM: int = 128
const SLOT_SIZE_LARGE: int = 160
const SLOT_SIZE_ICON: int = 128  # Main slot size

# Tab dimensions (doubled for 1280x720 reference)
const TAB_HEIGHT: float = 88.0
const TAB_MIN_WIDTH: float = 128.0
const TAB_MAX_WIDTH: float = 240.0

# Icon button sizes (for close, arrows, sort, etc.)
const ICON_BUTTON_SIZE: int = 80  # Standard icon-only button (close, arrows)
const ICON_BUTTON_SIZE_SMALL: int = 64  # Smaller icon buttons (sort, delete)

# Menu panel sizes (for modal menus like main menu, settings, save/load)
const MENU_PANEL_WIDTH: float = 600.0
const MENU_PANEL_HEIGHT: float = 800.0
const MENU_PANEL_WIDTH_SMALL: float = 480.0
const MENU_PANEL_HEIGHT_SMALL: float = 600.0

# Menu button sizes
const MENU_BUTTON_WIDTH: float = 400.0
const MENU_BUTTON_HEIGHT: float = 96.0

# Thumbnail/preview sizes
const THUMBNAIL_WIDTH: int = 160
const THUMBNAIL_HEIGHT: int = 100

# Slider dimensions
const SLIDER_HEIGHT: int = 48
const SLIDER_MIN_WIDTH: int = 300

# Portrait/character display area
const PORTRAIT_MARGIN_RIGHT: int = 200
const PORTRAIT_MARGIN_BOTTOM: int = 120

# Inset container padding (for recessed scroll areas)
const INSET_PADDING: int = 8

# Safe area defaults (can be overridden by actual device values)
const SAFE_AREA_TOP_DEFAULT: float = 0.0
const SAFE_AREA_BOTTOM_DEFAULT: float = 0.0
const SAFE_AREA_LEFT_DEFAULT: float = 0.0
const SAFE_AREA_RIGHT_DEFAULT: float = 0.0

# Notch/cutout handling (doubled for 1280x720 reference)
const NOTCH_HEIGHT_ESTIMATE: float = 88.0  # iOS notch approximate
const HOME_INDICATOR_HEIGHT: float = 68.0  # iOS home indicator

# Panel dimensions (doubled for 1280x720 reference)
const PANEL_MIN_WIDTH: float = 560.0
const PANEL_MAX_WIDTH: float = 960.0
const PANEL_PADDING: int = 32

# Party menu specific
const CHARACTER_PANEL_WIDTH_RATIO: float = 0.4  # 40% of screen width
const CONTENT_PANEL_WIDTH_RATIO: float = 0.6  # 60% of screen width
const EQUIPMENT_SLOTS_PER_ROW: int = 4
const INVENTORY_COLUMNS: int = 2

# Animation durations (seconds)
const ANIM_INSTANT: float = 0.0
const ANIM_FAST: float = 0.1
const ANIM_NORMAL: float = 0.2
const ANIM_SLOW: float = 0.3
const ANIM_VERY_SLOW: float = 0.5

# Z-index / layer ordering
const Z_BACKGROUND: int = 0
const Z_CONTENT: int = 10
const Z_PANEL: int = 20
const Z_MODAL: int = 50
const Z_OVERLAY: int = 80
const Z_TOOLTIP: int = 90
const Z_NOTIFICATION: int = 100

# Aspect ratio bounds for mobile (width:height as float)
const ASPECT_RATIO_MIN: float = 0.45  # ~9:20 (tall phones)
const ASPECT_RATIO_MAX: float = 0.65  # ~3:5 (shorter tablets)
