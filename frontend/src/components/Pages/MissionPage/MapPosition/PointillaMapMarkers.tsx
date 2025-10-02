import { Task, TaskStatus } from 'models/Task'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'
import L from 'leaflet'
import robotPictogram from 'mediaAssets/onshore_drone.svg'
import { Pose } from 'models/Pose'
import { tokens } from '@equinor/eds-tokens'

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

const getTaskMarker = (map: L.Map, task: Task) => {
    const color = getColorsFromTaskStatus(task.status)

    const taskMarker = L.circleMarker([task.inspection.inspectionTarget.y, task.inspection.inspectionTarget.x], {
        radius: 15,
        fillColor: color.fillColor,
        color: 'black',
        weight: 1,
        fillOpacity: 0.8,
    })
        .bindTooltip((task.taskOrder + 1).toString(), {
            permanent: true,
            direction: 'center',
            className: 'circleLabel',
        })
        .addTo(map)
    return taskMarker
}

export const getTaskMarkers = (map: L.Map, tasks: Task[]) => {
    return orderTasksByDrawOrder(tasks).map((task) => getTaskMarker(map, task))
}

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
