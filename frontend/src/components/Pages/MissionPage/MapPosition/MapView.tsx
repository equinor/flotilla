import { Card, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useApi } from "api/ApiCaller"
import { Mission } from 'models/Mission'
import { useEffect, useRef, useState } from "react"
import styled from "styled-components"
import { image, place } from '@equinor/eds-icons'
import NoMap from 'mediaAssets/NoMap.png'

Icon.add({ place,  })

interface MissionProps {
    mission: Mission
}

const MapCard = styled(Card)`
    height: 600px;
    width: 600px;
    padding: 16px;
    
`

const StyledMap = styled.canvas`
    object-fit: contain;
    object-position: center;
    min-height: 0;
    min-width: 0;
`

export function MapView({mission}:MissionProps){
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement("canvas"))
    const [mapAvailable, setMapAvailable] = useState<Boolean>(false)
    const apiCaller = useApi()
    var imageObjectURL:string

    useEffect(() => {
        apiCaller.getMap(mission.id).then((imageBlob) => {
            imageObjectURL = URL.createObjectURL(imageBlob);
            setMapAvailable(true)  
        }).catch(() => {
            imageObjectURL = NoMap
        })
        .then(() => {
            getMeta(imageObjectURL).then(img => {
            const mapCanvas = document.getElementById("MapCanvas") as HTMLCanvasElement;
            var context = mapCanvas?.getContext("2d")
            mapCanvas.width = img.width
            mapCanvas.height = img.height
            context?.drawImage(img, 0, 0)
            setMapCanvas(mapCanvas)
          })}
        )

    }, [])
    
    useEffect(() => {
        if(mapAvailable){
            PlaceTagsInMap(mission, mapCanvas)
        }
    }, [mapCanvas])

    const getMeta = async (url:string) => {
        const img = new Image();
        img.src = url;
        await img.decode();  
        return img
      };
    
    
    return(
        <MapCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <StyledMap id="MapCanvas" />
        </MapCard>
    )
}

function PlaceTagsInMap(mission: Mission, map: HTMLCanvasElement){
    if(mission.plannedTasks[0].tagPosition === null){return}
    var circleSize = 30;
    var tagNumber = 0;

    mission.plannedTasks.map(function(task)
        {
            var e1 = task.tagPosition.x
            var e2 = task.tagPosition.y
            var c1 = mission.map.transformationMatrices.c1
            var c2 = mission.map.transformationMatrices.c2
            var d1 = mission.map.transformationMatrices.d1
            var d2 = mission.map.transformationMatrices.d2
            var p1 = c1*e1+d1;
            var p2 = c2*e2+d2;
        
            var context = map.getContext("2d")
            if(context === null){return}
        
            context.beginPath()
            let path = new Path2D()
            path.arc(p1, map.height-p2, circleSize, 0, 2 * Math.PI);
        
            context.fillStyle = tokens.colors.interactive.primary__resting.hex
            context.strokeStyle = tokens.colors.text.static_icons__default.hex
            context.fill(path)
            context.stroke(path)
            context.font = '35pt Calibri';
            context.fillStyle = 'white';
            context.textAlign = 'center';
            context.fillText(tagNumber.toString(), p1, map.height-p2+circleSize/2);
            tagNumber += 1
        })
}

// function PlaceTagInMap(mission: Mission){
//     if (mission.robot.pose === undefined){
//         return
//     }
//     var e1 = mission.robot.pose.position.x
//     var e2 = mission.robot.pose.position.y
//     var c1 = mission.map.transformationMatrices.c1
//     var c2 = mission.map.transformationMatrices.c2
//     var d1 = mission.map.transformationMatrices.d1
//     var d2 = mission.map.transformationMatrices.d2
//     var p1 = c1*e1+d1;
//     var p2 = c2*e2+d2;
//     return(
//         <>
//         {p1}
//         </>
//     )
// }