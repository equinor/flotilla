import { Task, TaskStatus } from 'models/Task'
import L from 'leaflet'
import robotPictogram from 'mediaAssets/onshore_drone.svg'
import { Pose } from 'models/Pose'
import { tokens } from '@equinor/eds-tokens'
import { MissionTaskDefinition } from 'models/MissionDefinition'
import { Position } from 'models/Position'
import { InspectionData } from 'models/InspectionRecord'

const robotIcon = L.icon({
    iconUrl: robotPictogram,
    iconSize: [20, 30],
    iconAnchor: [10, 15],
})

const orderTasksByDrawOrder = (tasks: Task[]) => {
    const isOngoing = (task: Task) => task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
    const rank = (task: Task) => (isOngoing(task) ? 2 : task.status === TaskStatus.NotStarted ? 1 : 0)

    return [...tasks].sort((a, b) => {
        const ra = rank(a),
            rb = rank(b)
        if (ra !== rb) return ra - rb
        if (ra === 1) return b.taskOrder - a.taskOrder
        return a.taskOrder - b.taskOrder
    })
}

const getPositionMarker = (map: L.Map, pos: Position, index: number) => {
    const positionMarker = L.circleMarker([pos.y, pos.x], {
        radius: 15,
        fillColor: tokens.colors.ui.background__medium.hex,
        color: 'black',
        weight: 1,
        fillOpacity: 0.8,
    })
        .bindTooltip(index.toString(), {
            permanent: true,
            direction: 'center',
            className: 'circleLabel',
        })
        .addTo(map)
    return positionMarker
}

export const getTaskMarkers = (map: L.Map, tasks: Task[]) => {
    return orderTasksByDrawOrder(tasks).map((task) =>
        getPositionMarker(map, task.inspection.inspectionTarget, task.taskOrder)
    )
}

export const getTaskDefinitionMarkers = (map: L.Map, tasks: MissionTaskDefinition[]) => {
    return tasks.map((task, index) => getPositionMarker(map, task.targetPosition, index + 1))
}

export const getInspectionMarkers = (map: L.Map, inspections: InspectionData[]) =>
    inspections.map((inspection, index) => getPositionMarker(map, inspection.targetPosition, index + 1))

const getRobotAuraMarker = (map: L.Map, robotPose: Pose) => {
    const msFromMinToMax = 2000
    const minPixelRadius = 15
    const maxPixelRadius = 20
    const currentAuraRadius =
        ((new Date().getTime() % msFromMinToMax) * (maxPixelRadius - minPixelRadius)) / msFromMinToMax + minPixelRadius

    const auraMarker = L.circleMarker([robotPose.position.y, robotPose.position.x], {
        radius: currentAuraRadius,
        fillColor: tokens.colors.interactive.primary__resting.hex,
        weight: 0,
        fillOpacity: 0.6,
    }).addTo(map)

    return auraMarker
}

export const getRobotMarker = (map: L.Map, robotPose: Pose) => {
    const auraMarker = getRobotAuraMarker(map, robotPose)
    const backgroundMarker = L.circleMarker([robotPose.position.y, robotPose.position.x], {
        radius: 15,
        fillColor: 'white',
        color: 'black',
        weight: 1,
        fillOpacity: 1,
    }).addTo(map)
    const robotMarker = L.marker([robotPose.position.y, robotPose.position.x], {
        icon: robotIcon,
    }).addTo(map)

    return [auraMarker, backgroundMarker, robotMarker]
}
