import { MapContainer } from 'react-leaflet'
import styled from 'styled-components'
import 'leaflet/dist/leaflet.css'
import 'utils/leaflet-overrides.css'
import AuthTileLayer from 'pages/MissionPage/MapPosition/PointillaMap'
import { LeafletTooltipStyles, useInspectionsPlantMapData } from 'pages/MissionPage/MapPosition/PointillaMapView'
import { InspectionData } from 'models/InspectionRecord'

const FillMapContainer = styled(MapContainer)`
    flex: 1;
    min-height: 0;
    width: 100%;
    position: relative;
    z-index: 0;
`

interface DashboardInspectionsPlantMapProps {
    plantCode: string
    floorId: string
    inspections: InspectionData[]
}

export const DashboardInspectionsPlantMap = ({
    plantCode,
    floorId,
    inspections,
}: DashboardInspectionsPlantMapProps) => {
    const { mapInfo, setMap } = useInspectionsPlantMapData(plantCode, floorId, inspections, () => {})

    return (
        <>
            <LeafletTooltipStyles />
            <FillMapContainer ref={setMap} attributionControl={false}>
                {mapInfo && <AuthTileLayer mapInfo={mapInfo} />}
            </FillMapContainer>
        </>
    )
}
