type AnimationType = 'none' | 'fade' | 'flip' | 'slide' | 'zoom-in' | 'zoom-out';
type AnimationDirection = 'up' | 'down' | 'left' | 'right';

interface AnimationProps {
	type?: AnimationType,
	direction?: AnimationDirection
}
