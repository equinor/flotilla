import { tokens } from '@equinor/eds-tokens'
import { MapMetadata } from 'models/MapMetadata'
import { Pose } from 'models/Pose'
import { Task, TaskStatus } from 'models/Task'
import { getColorsFromTaskStatus } from './MarkerStyles'

interface ObjectPosition {
    x: number
    y: number
}

export const placeTagsInMap = (
    tasks: Task[],
    mapMetadata: MapMetadata,
    map: HTMLCanvasElement,
    currentTaskOrder: number
) => {
    const maxTaskOrder: number = Math.max(...tasks.map((task) => task.taskOrder))

    const orderedTasks = orderTasksByDrawOrder(tasks, currentTaskOrder, maxTaskOrder)
    const markerSize = calculateMarkerSize(map)

    orderedTasks.forEach((task) => {
        if (task.inspection === null) {
            const pixelPosition = calculateObjectPixelPosition(mapMetadata, task.robotPose.position)
            // Workaround for current bug in echo
            const order = task.taskOrder + 1
            drawTagMarker(pixelPosition[0], pixelPosition[1], map, order, markerSize, task.status)
        } else {
            const pixelPosition = calculateObjectPixelPosition(mapMetadata, task.inspection.inspectionTarget)
            // Workaround for current bug in echo
            const order = task.taskOrder + 1
            drawTagMarker(pixelPosition[0], pixelPosition[1], map, order, markerSize, task.status)
        }
    })
}

export const placeRobotInMap = (mapMetadata: MapMetadata, map: HTMLCanvasElement, robotPose: Pose) => {
    const pixelPosition: [number, number] = calculateObjectPixelPosition(mapMetadata, robotPose.position)
    const rad: number = calculateNavigatorAngle(robotPose)
    const markerSize = calculateMarkerSize(map)
    drawRobotMarker(pixelPosition[0], pixelPosition[1], map, markerSize)
    drawNavigator(pixelPosition[0], pixelPosition[1], map, markerSize, rad)
}

const calculateMarkerSize = (map: HTMLCanvasElement) => {
    const markerPercentage = 0.025
    const markerSize = map.width * markerPercentage
    return markerSize
}

const calculateObjectPixelPosition = (mapMetadata: MapMetadata, objectPosition: ObjectPosition): [number, number] => {
    const x1 = objectPosition.x
    const x2 = objectPosition.y

    const a1 = mapMetadata.transformationMatrices.c1
    const a2 = mapMetadata.transformationMatrices.c2
    const b1 = mapMetadata.transformationMatrices.d1
    const b2 = mapMetadata.transformationMatrices.d2

    const p1 = a1 * x1 + b1
    const p2 = a2 * x2 + b2
    return [p1, p2]
}

const orderTasksByDrawOrder = (tasks: Task[], currentTaskOrder: number, maxTaskOrder: number) => {
    const tasksWithDrawOrder = tasks.map((task) => {
        let drawOrder
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
    return tasksWithDrawOrder.map((taskWithDrawOrder) => taskWithDrawOrder.task)
}

const calculateNavigatorAngle = (currentRobotPose: Pose): number => {
    let rad = 2 * Math.atan2(currentRobotPose.orientation.z, currentRobotPose.orientation.w)
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

    const colors = getColorsFromTaskStatus(taskStatus)

    context.beginPath()
    const path = new Path2D()
    path.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)

    context.fillStyle = colors.fillColor
    context.strokeStyle = tokens.colors.text.static_icons__default.hex
    context.fill(path)
    context.stroke(path)
    context.font = circleSize + 'pt Calibri'
    context.fillStyle = colors.textColor
    context.textAlign = 'center'
    context.fillText(tagNumber.toString(), p1, map.height - p2 + circleSize / 2)
}

const drawRobotMarker = (p1: number, p2: number, map: HTMLCanvasElement, circleSize: number) => {
    const outerCircleRadius = circleSize * 1.1
    drawAura(p1, p2, map, outerCircleRadius)

    const context = map.getContext('2d')
    if (context === null) {
        return
    }

    context.beginPath()

    const outerCircle = new Path2D()
    outerCircle.arc(p1, map.height - p2, outerCircleRadius, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.text.static_icons__primary_white.hex
    context.fill(outerCircle)

    const innerCircle = new Path2D()
    innerCircle.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.fill(innerCircle)
}

const drawNavigator = (p1: number, p2: number, map: HTMLCanvasElement, markerSize: number, rad: number) => {
    const context = map.getContext('2d')
    if (context === null) {
        return
    }
    context.beginPath()
    context.fillStyle = 'white'
    context.strokeStyle = tokens.colors.text.static_icons__default.hex

    const navigationIcon = 'M4.5 20.79 12 2.5l7.5 18.29-.71.71-6.79-3-6.79 3-.71-.71Z'
    const navigationPath = new Path2D(navigationIcon)
    context.save()
    const scalingFactor = 0.05 * markerSize

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
    const pulseSizePixels = circleSize * 0.5
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
