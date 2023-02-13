import { Card, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useApi } from 'api/ApiCaller'
import { Mission } from 'models/Mission'
import { useEffect, useRef, useState } from 'react'
import styled from 'styled-components'
import { image, place } from '@equinor/eds-icons'
import NoMap from 'mediaAssets/NoMap.png'
import { MissionMap } from 'models/MissionMap'

Icon.add({ place })

interface MissionProps {
    mission: Mission
}

interface ObjectPosition {
    x: number
    y: number
}

const MapCard = styled(Card)`
    display: flex;
    height: 600px;
    width: 600px;
    padding: 16px;
`

const StyledMap = styled.canvas`
    object-fit: contain;
    max-height: 100%;
    max-width: 100%;
    margin: auto;
`

export function MapView({ mission }: MissionProps) {
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapAvailable, setMapAvailable] = useState<Boolean>(false)
    const apiCaller = useApi()
    var imageObjectURL: string

    useEffect(() => {
        apiCaller
            .getMap(mission.id)
            .then((imageBlob) => {
                imageObjectURL = URL.createObjectURL(imageBlob)
                setMapAvailable(true)
            })
            .catch(() => {
                imageObjectURL = NoMap
            })
            .then(() => {
                getMeta(imageObjectURL).then((img) => {
                    const mapCanvas = document.getElementById('MapCanvas') as HTMLCanvasElement
                    mapCanvas.width = img.width
                    mapCanvas.height = img.height
                    var context = mapCanvas?.getContext('2d')
                    context?.drawImage(img, 0, 0)
                    setMapCanvas(mapCanvas)
                    setMapImage(img)
                })
            })
    }, [])

    useEffect(() => {
        if (mapAvailable) {
            PlaceTagsInMap(mission, mapCanvas)
        }
    }, [mapCanvas])

    useEffect(() => {
        if (mapAvailable) {
            updateMap(mission, mapCanvas, mapImage)
        }
    }, [mission.robot.pose])

    const getMeta = async (url: string) => {
        const img = new Image()
        img.src = url
        await img.decode()
        return img
    }

    return (
        <MapCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <StyledMap id="MapCanvas" />
        </MapCard>
    )
}

function updateMap(mission: Mission, mapCanvas: HTMLCanvasElement, mapImage: HTMLImageElement) {
    var context = mapCanvas.getContext('2d')
    if (context === null) {
        return
    }
    context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
    context?.drawImage(mapImage, 0, 0)
    PlaceTagsInMap(mission, mapCanvas)
    PlaceRobotInMap(mission, mapCanvas)
}

function PlaceTagsInMap(mission: Mission, map: HTMLCanvasElement) {
    if (mission.plannedTasks[0].tagPosition === null) {
        return
    }
    var tagNumber = 1

    mission.plannedTasks.map(function (task) {
        var pixelPosition = calculateObjectPixelPosition(mission.map, task.tagPosition)
        drawTagMarker(pixelPosition[0], pixelPosition[1], map, tagNumber)
        tagNumber += 1
    })
}

function PlaceRobotInMap(mission: Mission, map: HTMLCanvasElement) {
    if (mission.robot.pose === undefined) {
        return
    }
    var pixelPosition = calculateObjectPixelPosition(mission.map, mission.robot.pose.position)
    drawRobotMarker(pixelPosition[0], pixelPosition[1], map)
}

function calculateObjectPixelPosition(missionMap: MissionMap, objectPosition: ObjectPosition) {
    var e1 = objectPosition.x
    var e2 = objectPosition.y
    var c1 = missionMap.transformationMatrices.c1
    var c2 = missionMap.transformationMatrices.c2
    var d1 = missionMap.transformationMatrices.d1
    var d2 = missionMap.transformationMatrices.d2
    var p1 = c1 * e1 + d1
    var p2 = c2 * e2 + d2
    return new Array(p1, p2)
}

function drawTagMarker(p1: number, p2: number, map: HTMLCanvasElement, tagNumber: number) {
    var circleSize = 30
    var context = map.getContext('2d')
    if (context === null) {
        return
    }

    context.beginPath()
    let path = new Path2D()
    path.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)

    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.strokeStyle = tokens.colors.text.static_icons__default.hex
    context.fill(path)
    context.stroke(path)
    context.font = '35pt Calibri'
    context.fillStyle = 'white'
    context.textAlign = 'center'
    context.fillText(tagNumber.toString(), p1, map.height - p2 + circleSize / 2)
}
function drawRobotMarker(p1: number, p2: number, map: HTMLCanvasElement) {
    var circleSize = 20

    var context = map.getContext('2d')
    if (context === null) {
        return
    }

    context.beginPath()

    let outerAura = new Path2D()
    outerAura.arc(p1, map.height - p2, circleSize + 15, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.globalAlpha = 0.5
    context.fill(outerAura)
    context.globalAlpha = 1

    let outerCircle = new Path2D()
    outerCircle.arc(p1, map.height - p2, circleSize + 5, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.text.static_icons__primary_white.hex
    context.fill(outerCircle)

    let innerCircle = new Path2D()
    innerCircle.arc(p1, map.height - p2, circleSize, 0, 2 * Math.PI)
    context.fillStyle = tokens.colors.interactive.primary__resting.hex
    context.fill(innerCircle)
}
