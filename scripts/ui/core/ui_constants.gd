class_name UIConstants
extends RefCounted

## UIConstants
## Static constants for UI sizing, spacing, and mobile-friendly values
## All sizes are in logical pixels (dp/pt equivalent)

# Reference design dimensions
# Designed for 360dp width (Android minimum) and 375pt width (iOS minimum)
const REFERENCE_WIDTH: float = 360.0
const REFERENCE_HEIGHT: float = 640.0

# Minimum touch target sizes (per platform guidelines)
# Android: 48dp minimum, iOS: 44pt minimum
const MIN_TOUCH_TARGET: float = 48.0
const MIN_TOUCH_TARGET_SMALL: float = 44.0  # For less critical elements

# Icon sizes
const ICON_SIZE_SMALL: int = 16
const ICON_SIZE_MEDIUM: int = 24
const ICON_SIZE_LARGE: int = 32
const ICON_SIZE_XLARGE: int = 48

# Button sizes
const BUTTON_HEIGHT_SMALL: float = 36.0
const BUTTON_HEIGHT_MEDIUM: float = 44.0
const BUTTON_HEIGHT_LARGE: float = 56.0
const BUTTON_MIN_WIDTH: float = 64.0

# Slot sizes for inventory/equipment
const SLOT_SIZE_SMALL: int = 48
const SLOT_SIZE_MEDIUM: int = 64
const SLOT_SIZE_LARGE: int = 80
const SLOT_SIZE_ICON: int = 96  # 1.5x for better equipment icon visibility

# Tab dimensions
const TAB_HEIGHT: float = 44.0
const TAB_MIN_WIDTH: float = 64.0
const TAB_MAX_WIDTH: float = 120.0

# Safe area defaults (can be overridden by actual device values)
const SAFE_AREA_TOP_DEFAULT: float = 0.0
const SAFE_AREA_BOTTOM_DEFAULT: float = 0.0
const SAFE_AREA_LEFT_DEFAULT: float = 0.0
const SAFE_AREA_RIGHT_DEFAULT: float = 0.0

# Notch/cutout handling
const NOTCH_HEIGHT_ESTIMATE: float = 44.0  # iOS notch approximate
const HOME_INDICATOR_HEIGHT: float = 34.0  # iOS home indicator

# Spacing scale (follows 4px grid)
const SPACING_NONE: int = 0
const SPACING_XXXS: int = 2
const SPACING_XXS: int = 4
const SPACING_XS: int = 8
const SPACING_SM: int = 12
const SPACING_MD: int = 16
const SPACING_LG: int = 24
const SPACING_XL: int = 32
const SPACING_XXL: int = 48
const SPACING_XXXL: int = 64

# Border radii
const RADIUS_NONE: int = 0
const RADIUS_SM: int = 4
const RADIUS_MD: int = 8
const RADIUS_LG: int = 12
const RADIUS_XL: int = 16
const RADIUS_FULL: int = 9999  # For pill shapes

# Border widths
const BORDER_THIN: int = 1
const BORDER_MEDIUM: int = 2
const BORDER_THICK: int = 4

# Panel dimensions
const PANEL_MIN_WIDTH: float = 280.0
const PANEL_MAX_WIDTH: float = 480.0
const PANEL_PADDING: int = 16

# Party menu specific
const CHARACTER_PANEL_WIDTH_RATIO: float = 0.4  # 40% of screen width
const CONTENT_PANEL_WIDTH_RATIO: float = 0.6  # 60% of screen width
const EQUIPMENT_SLOTS_PER_ROW: int = 4
const INVENTORY_COLUMNS: int = 4

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
