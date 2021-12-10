export interface Position {
  x: number;
  y: number;
  z: number;
}

export interface Orientation {
  x: number;
  y: number;
  z: number;
  w: number;
}

export interface Pose {
  position: Position;
  orientation: Orientation;
}

export const defaultPosition: Position = {
  x: 1,
  y: 1,
  z: 1,
};

export const defaultOrientation: Orientation = {
  x: 0,
  y: 0,
  z: 0,
  w: 1,
};

export const defaultPose: Pose = {
  position: defaultPosition,
  orientation: defaultOrientation,
};
