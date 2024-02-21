using UnityEngine;

namespace BHTree
{
    public class BarnesHutTree
    {
        public double Theta = 0.1;
        public Body body;
        public Quad quad;
        public BarnesHutTree NW;
        public BarnesHutTree NE;
        public BarnesHutTree SW;
        public BarnesHutTree SE;

        public BarnesHutTree(Quad quad)
        {
            this.quad = quad;
            body = null;
            NW = null;
            NE = null;
            SW = null;
            SE = null;
        }

        public void insert(Body b)
        {
            if (body == null)
            {
                body = b;
                return;
            }

            if (!isExternal())
            {
                body = body.plus(b);

                putBody(b);
            }
            else
            {
                NW = new BarnesHutTree(quad.NW());
                NE = new BarnesHutTree(quad.NE());
                SE = new BarnesHutTree(quad.SE());
                SW = new BarnesHutTree(quad.SW());

                // recursively insert both this body and Body b into the appropriate quadrant
                putBody(body);
                putBody(b);
                // update the center-of-mass and total mass
                body = body.plus(b);
            }
        }

        public void putBody(Body b)
        {
            if (b.inCheck(quad.NW()))
                NW.insert(b);
            else if (b.inCheck(quad.NE()))
                NE.insert(b);
            else if (b.inCheck(quad.SE()))
                SE.insert(b);
            else if (b.inCheck(quad.SW()))
                SW.insert(b);
        }


        public bool isExternal()
        {
            // a node is external iff all four children are null
            return NW == null && NE == null && SW == null && SE == null;
        }

        public void updateForce(Body b)
        {
            if (body == null || b.Equals(body))
                return;
            // if the current node is external, update net force acting on b
            if (isExternal())
                b.addForce(body);
            // for internal nodes
            else
            {
                // width of region represented by internal node
                double s = quad.getLength();
                // distance between Body b and this node's center-of-mass
                double d = body.distanceTo(b);
                // compare ratio (s / d) to threshold value Theta
                if ((s / d) < Theta)
                    b.addForce(body); // b is far away
                // recurse on each of current node's children
                else
                {
                    NW.updateForce(b);
                    NE.updateForce(b);
                    SW.updateForce(b);
                    SE.updateForce(b);
                }
            }
        }
    }

    public class Quad
    {

        public double xmid;
        public double ymid;
        public double length;

        public Quad(double xmid, double ymid, double length)
        {
            this.xmid = xmid;
            this.ymid = ymid;
            this.length = length;
        }

        public double getLength()
        {
            return length;
        }

        public bool contains(double x, double y)
        {
            double halfLen = length / 2.0;
            return (x <= xmid + halfLen &&
                    x >= xmid - halfLen &&
                    y <= ymid + halfLen &&
                    y >= ymid - halfLen);
        }

        public Quad NW()
        {
            double x = xmid - length / 4.0;
            double y = ymid + length / 4.0;
            double len = length / 2.0;
            Quad NW = new Quad(x, y, len);
            return NW;
        }

        public Quad NE()
        {
            double x = xmid + length / 4.0;
            double y = ymid + length / 4.0;
            double len = length / 2.0;
            Quad NE = new Quad(x, y, len);
            return NE;
        }

        public Quad SW()
        {
            double x = xmid - length / 4.0;
            double y = ymid - length / 4.0;
            double len = length / 2.0;
            Quad SW = new Quad(x, y, len);
            return SW;
        }

        public Quad SE()
        {
            double x = xmid + length / 4.0;
            double y = ymid - length / 4.0;
            double len = length / 2.0;
            Quad SE = new Quad(x, y, len);
            return SE;
        }
    }

    public class Body
    {
        public double G;
        public double rx, ry; // position
        public double fx, fy; // force
        public double mass; // mass

        public Body(double rx, double ry, double mass, double G)
        {
            this.rx = rx;
            this.ry = ry;
            this.mass = mass;
            this.G = G;
        }

        public void update(double rx, double ry)
        {
            this.rx = rx;
            this.ry = ry;
        }

        public double distanceTo(Body b)
        {
            double dx = rx - b.rx;
            double dy = ry - b.ry;
            return Mathf.Sqrt((float)(dx * dx + dy * dy));
        }

        public void resetForce()
        {
            fx = 0.0;
            fy = 0.0;
        }

        public void addForce(Body b)
        {
            if (distanceTo(b) > 0.75f)
            {
                Body a = this;
                double dx = b.rx - a.rx;
                double dy = b.ry - a.ry;
                double dist = Mathf.Sqrt((float)(dx * dx + dy * dy));
                double F = G * a.mass * b.mass / (dist * dist);
                a.fx += F * dx / dist;
                a.fy += F * dy / dist;
            }
        }

        public bool inCheck(Quad q)
        {
            return q.contains(rx, ry);
        }

        public Body plus(Body b)
        {
            Body a = this;
            double m = a.mass + b.mass;
            double x = (a.rx * a.mass + b.rx * b.mass) / m;
            double y = (a.ry * a.mass + b.ry * b.mass) / m;
            return new Body(x, y, m, a.G);
        }
    }
}