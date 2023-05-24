import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { MissionMap } from 'models/MissionMap'
import { Pose } from 'models/Pose'
import { Task, TaskStatus } from 'models/Task'
import { GetColorsFromTaskStatus } from '../MarkerStyles'
import { Position } from 'models/Position'

interface ObjectPosition {
    x: number
    y: number
}

export const PlaceTagsInMap = (mission: Mission, map: HTMLCanvasElement, currentTaskOrder?: number) => {
    const maxTaskOrder: number = Math.max(
        ...mission.tasks.map((task) => {
            return task.taskOrder
        })
    )
    if (currentTaskOrder === undefined) {
        currentTaskOrder = mission.isCompleted ? maxTaskOrder + 1 : 0
    }

    const orderedTasks = orderTasksByDrawOrder(mission.tasks, currentTaskOrder, maxTaskOrder)
    orderedTasks.map(function (task) {
        if (task.inspectionTarget) {
            const pixelPosition = calculateObjectPixelPosition(mission.map!, task.inspectionTarget)
            drawTagMarker(pixelPosition[0], pixelPosition[1], map, task.taskOrder + 1, 30, task.status)
        }
    })
}

export const PlacePositionsInMap = (missionMap: MissionMap, map: HTMLCanvasElement, positions: Position[]) => {
    var index = 1
    positions.map(function (position) {
        const pixelPosition = calculateObjectPixelPosition(missionMap, position)
        drawTagMarker(pixelPosition[0], pixelPosition[1], map, index, 30, TaskStatus.NotStarted)
        index += 1
    })
}

export const PlaceRobotInMap = (
    mission: Mission,
    map: HTMLCanvasElement,
    currentRobotPose: Pose,
    previousRobotPose: Pose
) => {
    if (mission.robot.pose === undefined) {
        return
    }
    const pixelPosition = calculateObjectPixelPosition(mission.map!, currentRobotPose.position)
    const rad = calculateNavigatorAngle(currentRobotPose, previousRobotPose)
    drawRobotMarker(pixelPosition[0], pixelPosition[1], map, 22)
    drawNavigator(pixelPosition[0], pixelPosition[1], map, rad)
}

const orderTasksByDrawOrder = (tasks: Task[], currentTaskOrder: number, maxTaskOrder: number) => {
    let tasksWithDrawOrder = tasks.map(function (task) {
        var drawOrder
        if (task.taskOrder === currentTaskOrder) {
            drawOrder = maxTaskOrder
        } else if (task.taskOrder < currentTaskOrder) {
            drawOrder = task.taskOrder
        } else {
            drawOrder = currentTaskOrder + (maxTaskOrder - task.taskOrder)
        }
        return { task, drawOrder }
    })

    tasksWithDrawOrder.sort((a, b) => a.drawOrder - b.drawOrder)
    return tasksWithDrawOrder.map(function (taskWithDrawOrder) {
        return taskWithDrawOrder.task
    })
}

const calculateObjectPixelPosition = (missionMap: MissionMap, objectPosition: ObjectPosition) => {
    const x1 = objectPosition.x
    const x2 = objectPosition.y
    const a1 = missionMap.transformationMatrices.c1
    const a2 = missionMap.transformationMatrices.c2
    const b1 = missionMap.transformationMatrices.d1
    const b2 = missionMap.transformationMatrices.d2
    const p1 = a1 * x1 + b1
    const p2 = a2 * x2 + b2
    return new Array(p1, p2)
}

const calculateNavigatorAngle = (currentRobotPose: Pose, previousRobotPose: Pose) => {
    let rad = Math.atan2(
        currentRobotPose.position.y - previousRobotPose.position.y,
        currentRobotPose.position.x - previousRobotPose.position.x
    )
    rad = -rad + Math.PI / 2
    return rad
}

const drawTagMarker = (
    p1: number,
    p2: number,
    map: HTMLCanvasElement,
    tagNumber: number,
    circleSize: number,
    taskStatus: TaskStatus
) => {
    const context = map.getContext('2d')
    if (context === null) {
        return
    }

    const colors = GetColorsFromTaskStatus(taskStatus)

    context.beginPath()
    const path = new Path2D()
    path.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)

    context.fillStyle = colors.fillColor
    context.strokeStyle = tokens.colors.text.static_icons__default.hex
    context.fill(path)
    context.stroke(path)
    context.font = '35pt Calibri'
    context.fillStyle = colors.textColor
    context.textAlign = 'center'
    context.fillText(tagNumber.toString(), p1, map.height - p2 + circleSize / 2)
}

const drawRobotMarker = (p1: number, p2: number, map: HTMLCanvasElement, circleSize: number) => {
    drawAura(p1, p2, map, circleSize)

    const context = map.getContext('2d')
    if (context === null) {
        return
    }

    context.beginPath()

    const outerCircle = new Path2D()
    outerCircle.arc(p1, map.height - p2, circleSize + 5, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.text.static_icons__primary_white.hex
    context.fill(outerCircle)

    let innerCircle = new Path2D()
    innerCircle.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.fill(innerCircle)
}

const drawNavigator = (p1: number, p2: number, map: HTMLCanvasElement, rad: number) => {
    const context = map.getContext('2d')
    if (context === null) {
        return
    }
    const time = new Date()
    context.beginPath()
    context.fillStyle = 'white'
    context.strokeStyle = tokens.colors.text.static_icons__default.hex

    const navigationIcon = 'M4.5 20.79 12 2.5l7.5 18.29-.71.71-6.79-3-6.79 3-.71-.71Z'
    const navigationPath = new Path2D(navigationIcon)
    context.save()
    const scalingFactor = 1.2

    context.translate(p1, map.height - p2)
    context.rotate(rad)
    context.translate(-12 * scalingFactor, -12 * scalingFactor - 2)
    context.scale(scalingFactor, scalingFactor)

    context.fill(navigationPath)
    context.restore()
}

const drawAura = (x: number, y: number, map: HTMLCanvasElement, circleSize: number) => {
    const context = map.getContext('2d')
    if (context === null) {
        return
    }

    const pulseDurationMilliseconds = 1500
    const pulseSizePixels = 35
    let timer = new Date().getTime()
    // reset timer every pulseDurationMilliseconds
    timer = timer % pulseDurationMilliseconds

    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.beginPath()
    context.arc(
        x,
        map.height - y,
        circleSize + pulseSizePixels * Math.sin((Math.PI / 2) * (timer / pulseDurationMilliseconds)),
        0,
        2 * Math.PI
    )
    context.globalAlpha = 1 - Math.sin((Math.PI / 2) * (timer / pulseDurationMilliseconds))
    context.fill()
    context.globalAlpha = 1
}
