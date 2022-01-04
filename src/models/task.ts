import { Pose, Position } from './pose'

export class Task {
    task_number: number
    constructor(task_number: number) {
        this.task_number = task_number
    }
}

export class DriveToPose extends Task {
    name: string
    pose: Pose
    constructor(pose: Pose, task_number: number) {
        super(task_number)
        this.name = 'drive_to_pose'
        this.pose = pose
    }
}

export class TakeImage extends Task {
    name: string
    target: Position
    constructor(target: Position, task_number: number) {
        super(task_number)
        this.name = 'take_image'
        this.target = target
    }
}

export class TakeThermalImage extends Task {
    name: string
    target: Position
    constructor(target: Position, task_number: number) {
        super(task_number)
        this.name = 'take_thermal_image'
        this.target = target
    }
}
