import styled from 'styled-components'
import compass from 'mediaAssets/compass.png'

const StyledCompass = styled.img<{ $height?: string }>`
    height: 70px;
    padding: 0px 10px;
`

export const MapCompass = () => {
    return <StyledCompass src={compass} />
}
