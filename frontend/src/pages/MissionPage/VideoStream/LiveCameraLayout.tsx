import styled from 'styled-components'

export const LiveCameraStage = styled.div`
    position: relative;
    min-width: 0;
    overflow: hidden;
`

export const LiveCameraRow = styled.div<{ $visible: boolean }>`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1rem;
    padding: 1rem 0;
    min-width: 0;
    ${({ $visible }) =>
        !$visible &&
        `
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            visibility: hidden;
        `}
`

export const LiveCamera = styled.div<{ $aspectRatio: number }>`
    flex: ${({ $aspectRatio }) => $aspectRatio} 1 ${({ $aspectRatio }) => $aspectRatio * 12}rem;
    min-width: min(100%, ${({ $aspectRatio }) => $aspectRatio * 12}rem);
    max-width: min(100%, ${({ $aspectRatio }) => $aspectRatio * 20}rem);
`

export const LiveCameraViewport = styled.div<{ $aspectRatio: number }>`
    position: relative;
    aspect-ratio: ${({ $aspectRatio }) => $aspectRatio};
    background: black;
`

export const LiveCameraStatus = styled.div`
    position: absolute;
    inset: 0;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 0.75rem;
    padding: 0.75rem;
    background: rgb(0 0 0 / 60%);
    text-align: center;
`
